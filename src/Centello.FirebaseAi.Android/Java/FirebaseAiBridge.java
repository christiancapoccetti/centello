package centello.firebaseai;

import android.content.Context;
import com.google.common.util.concurrent.ListenableFuture;
import com.google.firebase.FirebaseApp;
import com.google.firebase.FirebaseOptions;
import com.google.firebase.ai.FirebaseAI;
import com.google.firebase.ai.java.GenerativeModelFutures;
import com.google.firebase.ai.type.Content;
import com.google.firebase.ai.type.ContentBlockedException;
import com.google.firebase.ai.type.FinishReason;
import com.google.firebase.ai.type.GenerateContentResponse;
import com.google.firebase.ai.type.GenerationConfig;
import com.google.firebase.ai.type.GenerativeBackend;
import com.google.firebase.ai.type.InvalidAPIKeyException;
import com.google.firebase.ai.type.PermissionMissingException;
import com.google.firebase.ai.type.PromptBlockedException;
import com.google.firebase.ai.type.QuotaExceededException;
import com.google.firebase.ai.type.RequestOptions;
import com.google.firebase.ai.type.RequestTimeoutException;
import com.google.firebase.ai.type.ResponseStoppedException;
import com.google.firebase.ai.type.Schema;
import com.google.firebase.ai.type.SerializationException;
import com.google.firebase.ai.type.ServerException;
import com.google.firebase.ai.type.UsageMetadata;
import com.google.firebase.appcheck.FirebaseAppCheck;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.concurrent.CancellationException;
import java.util.concurrent.ExecutionException;

public final class FirebaseAiBridge {
    private static final String MODEL = "gemini-3.5-flash-lite";
    private static final long REQUEST_TIMEOUT_MS = 60_000;
    private static GenerativeModelFutures model;

    private FirebaseAiBridge() {}

    public static FirebaseAiRequest start(Context context, String applicationId, String apiKey,
            String projectId, String senderId, String mimeType, byte[] bytes, String prompt,
            FirebaseAiCallback callback) {
        final ListenableFuture<GenerateContentResponse> future;
        try {
            Content content = new Content.Builder().addText(prompt).addInlineData(bytes, mimeType).build();
            future = getModel(context, applicationId, apiKey, projectId, senderId).generateContent(content);
        } catch (RuntimeException error) {
            callback.onError(classify(error));
            return () -> {};
        }

        future.addListener(() -> complete(future, callback), Runnable::run);
        return () -> future.cancel(true);
    }

    private static synchronized GenerativeModelFutures getModel(Context context, String applicationId,
            String apiKey, String projectId, String senderId) {
        if (model != null) {
            return model;
        }

        FirebaseOptions options = new FirebaseOptions.Builder()
                .setApplicationId(applicationId)
                .setApiKey(apiKey)
                .setProjectId(projectId)
                .setGcmSenderId(senderId)
                .build();

        FirebaseApp app = FirebaseApp.initializeApp(context, options);
        try {
            AppCheckProvider.install(FirebaseAppCheck.getInstance(app));
            FirebaseAI ai = FirebaseAI.getInstance(app, GenerativeBackend.googleAI(), true);
            model = GenerativeModelFutures.from(ai.generativeModel(
                    MODEL, generationConfig(), null, null, null, null, new RequestOptions(REQUEST_TIMEOUT_MS)));
            return model;
        } catch (RuntimeException error) {
            app.delete();
            throw error;
        }
    }

    private static GenerationConfig generationConfig() {
        Map<String, Schema> properties = new LinkedHashMap<>();
        properties.put("amount", Schema.numDouble(null, true));
        properties.put("categoryKey", Schema.str(null, true));
        properties.put("tagKey", Schema.str(null, true));
        properties.put("date", Schema.str(null, true));
        properties.put("note", Schema.str(null, true));
        properties.put("heard", Schema.str(null, true));
        return new GenerationConfig.Builder()
                .setMaxOutputTokens(2048)
                .setResponseMimeType("application/json")
                .setResponseSchema(Schema.obj(properties))
                .build();
    }

    private static void complete(ListenableFuture<GenerateContentResponse> future, FirebaseAiCallback callback) {
        final String json;
        final UsageMetadata usage;
        try {
            GenerateContentResponse response = future.get();
            json = response.getText();
            usage = response.getUsageMetadata();
        } catch (CancellationException error) {
            callback.onError("cancelled");
            return;
        } catch (ExecutionException error) {
            callback.onError(classify(error.getCause()));
            return;
        } catch (InterruptedException error) {
            Thread.currentThread().interrupt();
            callback.onError("cancelled");
            return;
        } catch (RuntimeException error) {
            callback.onError(classify(error));
            return;
        }

        if (json == null || json.trim().isEmpty()) {
            callback.onError("invalid_response");
            return;
        }

        Integer candidates = usage == null ? null : usage.getCandidatesTokenCount();
        int output = candidates == null ? -1 : candidates + usage.getThoughtsTokenCount();
        callback.onResult(json, usage == null ? -1 : usage.getPromptTokenCount(), output);
    }

    private static String classify(Throwable error) {
        if (error instanceof PromptBlockedException || error instanceof ContentBlockedException) {
            return "blocked";
        }
        if (error instanceof PermissionMissingException || error instanceof InvalidAPIKeyException) {
            return "forbidden";
        }
        if (error instanceof QuotaExceededException) {
            return "rate_limited";
        }
        if (error instanceof RequestTimeoutException) {
            return "timeout";
        }
        if (error instanceof ResponseStoppedException) {
            GenerateContentResponse response = ((ResponseStoppedException) error).getResponse();
            if (!response.getCandidates().isEmpty()) {
                FinishReason reason = response.getCandidates().get(0).getFinishReason();
                if (reason == FinishReason.SAFETY || reason == FinishReason.RECITATION
                        || reason == FinishReason.BLOCKLIST || reason == FinishReason.PROHIBITED_CONTENT
                        || reason == FinishReason.SPII) {
                    return "blocked";
                }
            }
            return "invalid_response";
        }
        if (error instanceof SerializationException) {
            return "invalid_response";
        }
        // The SDK preserves the backend message but drops the HTTP status on these errors.
        if (error instanceof ServerException && error.getMessage() != null) {
            String message = error.getMessage();
            if (message.startsWith("The caller does not have permission")
                    || message.startsWith("Requests to this API firebasevertexai.googleapis.com")
                    || message.startsWith("Firebase App Check token is invalid")
                    || message.startsWith("Firebase AI Logic has been deactivated")
                    || message.startsWith("To access this model, you must enforce Firebase App Check")) {
                return "forbidden";
            }
            if (message.startsWith("Resource exhausted") || message.startsWith("Too many requests")
                    || message.startsWith("Rate limit exceeded")) {
                return "rate_limited";
            }
        }
        if (error instanceof CancellationException) {
            return "cancelled";
        }
        return "unavailable";
    }
}
