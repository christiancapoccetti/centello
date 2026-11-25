package centello.firebaseai;

import android.content.Context;
import android.util.Log;
import com.google.android.gms.tasks.Task;
import com.google.android.gms.tasks.Tasks;
import com.google.firebase.FirebaseApp;
import com.google.firebase.remoteconfig.ConfigUpdate;
import com.google.firebase.remoteconfig.ConfigUpdateListener;
import com.google.firebase.remoteconfig.ConfigUpdateListenerRegistration;
import com.google.firebase.remoteconfig.FirebaseRemoteConfig;
import com.google.firebase.remoteconfig.FirebaseRemoteConfigException;
import com.google.firebase.remoteconfig.FirebaseRemoteConfigServerException;
import com.google.firebase.remoteconfig.FirebaseRemoteConfigSettings;
import com.google.firebase.remoteconfig.FirebaseRemoteConfigValue;
import java.util.concurrent.Executor;
import java.util.concurrent.atomic.AtomicBoolean;

public final class FirebaseRemoteConfigBridge {
    private static final String TAG = "CentelloRemoteConfig";
    private static final String SURVEY_URL = "ai_interest_form_url";
    private static final String REVOKED_INVITE_IDS = "revoked_invite_ids";
    private static final long ORDINARY_FETCH_INTERVAL_SECONDS = 12 * 60 * 60;
    private static final Executor DIRECT = Runnable::run;
    private final FirebaseRemoteConfig config;
    private final Task<Void> configured;
    private final AtomicBoolean attemptedEmptyCacheRecovery = new AtomicBoolean();
    private ConfigUpdateListenerRegistration registration;

    private FirebaseRemoteConfigBridge(FirebaseRemoteConfig config) {
        this.config = config;
        configured = config.setConfigSettingsAsync(new FirebaseRemoteConfigSettings.Builder()
                .setFetchTimeoutInSeconds(5)
                .build());
    }

    public static FirebaseRemoteConfigBridge create(Context context, String applicationId, String apiKey,
            String projectId, String senderId) {
        FirebaseApp app = CentelloFirebaseApp.get(context, applicationId, apiKey, projectId, senderId);
        return new FirebaseRemoteConfigBridge(FirebaseRemoteConfig.getInstance(app));
    }

    public void listen(FirebaseRemoteConfigCallback updates) {
        if (registration != null) {
            return;
        }

        registration = config.addOnConfigUpdateListener(new RealtimeListener(updates));
    }

    public void stopListening() {
        if (registration != null) {
            registration.remove();
            registration = null;
        }
    }

    public void fetch(boolean force, FirebaseRemoteConfigCallback callback) {
        configured.onSuccessTask(DIRECT, unused -> fetchAndActivate(force)).addOnCompleteListener(DIRECT, result -> {
            if (result.isSuccessful()) {
                Log.i(TAG, "Activation completed: activated=" + Boolean.TRUE.equals(result.getResult()));
                publish(callback);
            } else {
                logFailure("Fetch or activation (force=" + force + ")", result.getException());
                callback.onSettings(null, null);
            }
        });
    }

    private Task<Boolean> fetchAndActivate(boolean force) {
        long previousFetchTime = config.getInfo().getFetchTimeMillis();
        return config.fetch(force ? 0 : ORDINARY_FETCH_INTERVAL_SECONDS).onSuccessTask(DIRECT, fetched -> {
            boolean fresh = config.getInfo().getFetchTimeMillis() != previousFetchTime;
            Log.i(TAG, "Fetch completed: force=" + force + " fresh=" + fresh);
            // A clock rollback can otherwise make fetch(0) return a cached value.
            if (force && !fresh) {
                return Tasks.forException(new IllegalStateException("No fresh Remote Config response"));
            }

            return config.activate().onSuccessTask(DIRECT, activated -> {
                // An install may have cached the template before either access parameter was published.
                if (!force && !fresh && !isRemote(SURVEY_URL) && !isRemote(REVOKED_INVITE_IDS)
                        && attemptedEmptyCacheRecovery.compareAndSet(false, true)) {
                    Log.i(TAG, "Retrying an empty cached access template.");
                    return fetchAndActivate(true);
                }

                return Tasks.forResult(activated);
            });
        });
    }

    private boolean isRemote(String key) {
        return config.getValue(key).getSource() == FirebaseRemoteConfig.VALUE_SOURCE_REMOTE;
    }

    private void publish(FirebaseRemoteConfigCallback callback) {
        FirebaseRemoteConfigValue survey = config.getValue(SURVEY_URL);
        FirebaseRemoteConfigValue revokedInviteIds = config.getValue(REVOKED_INVITE_IDS);
        boolean surveyRemote = survey.getSource() == FirebaseRemoteConfig.VALUE_SOURCE_REMOTE;
        boolean revocationsRemote = revokedInviteIds.getSource() == FirebaseRemoteConfig.VALUE_SOURCE_REMOTE;
        String surveyUrl = survey.asString();
        Log.i(TAG, "Activated values: surveyRemote=" + surveyRemote + " surveyPresent=" + !surveyUrl.isEmpty()
                + " revocationsRemote=" + revocationsRemote);
        callback.onSettings(surveyUrl, revocationsRemote ? revokedInviteIds.asString() : null);
    }

    private static void logFailure(String operation, Exception error) {
        String status = error == null ? "cancelled" : error.getClass().getSimpleName();
        if (error instanceof FirebaseRemoteConfigServerException) {
            status += " http=" + ((FirebaseRemoteConfigServerException) error).getHttpStatusCode();
        }
        Log.w(TAG, operation + " failed: " + status);
    }

    private final class RealtimeListener implements ConfigUpdateListener {
        private final FirebaseRemoteConfigCallback callback;

        private RealtimeListener(FirebaseRemoteConfigCallback callback) {
            this.callback = callback;
        }

        @Override
        public void onUpdate(ConfigUpdate update) {
            if (update.getUpdatedKeys().contains(SURVEY_URL) || update.getUpdatedKeys().contains(REVOKED_INVITE_IDS)) {
                config.activate().addOnCompleteListener(DIRECT, result -> {
                    if (result.isSuccessful() && Boolean.TRUE.equals(result.getResult())) {
                        publish(callback);
                    } else {
                        if (!result.isSuccessful()) {
                            logFailure("Realtime activation", result.getException());
                        }
                        callback.onSettings(null, null);
                    }
                });
            }
        }

        @Override
        public void onError(FirebaseRemoteConfigException error) {
            logFailure("Realtime listener", error);
        }
    }
}
