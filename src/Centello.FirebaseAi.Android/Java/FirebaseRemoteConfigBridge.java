package centello.firebaseai;

import android.content.Context;
import com.google.android.gms.tasks.Task;
import com.google.android.gms.tasks.Tasks;
import com.google.firebase.FirebaseApp;
import com.google.firebase.remoteconfig.ConfigUpdate;
import com.google.firebase.remoteconfig.ConfigUpdateListener;
import com.google.firebase.remoteconfig.ConfigUpdateListenerRegistration;
import com.google.firebase.remoteconfig.FirebaseRemoteConfig;
import com.google.firebase.remoteconfig.FirebaseRemoteConfigException;
import com.google.firebase.remoteconfig.FirebaseRemoteConfigSettings;
import com.google.firebase.remoteconfig.FirebaseRemoteConfigValue;
import java.util.concurrent.Executor;

public final class FirebaseRemoteConfigBridge {
    private static final String SURVEY_URL = "ai_interest_form_url";
    private static final String REVOKED_INVITE_IDS = "revoked_invite_ids";
    private static final long ORDINARY_FETCH_INTERVAL_SECONDS = 12 * 60 * 60;
    private static final Executor DIRECT = Runnable::run;
    private final FirebaseRemoteConfig config;
    private final Task<Void> configured;
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
        configured.onSuccessTask(DIRECT, unused -> {
            long previousFetchTime = config.getInfo().getFetchTimeMillis();
            return config.fetch(force ? 0 : ORDINARY_FETCH_INTERVAL_SECONDS)
                    .onSuccessTask(DIRECT, fetched -> {
                        // A clock rollback can otherwise make fetch(0) return a cached value.
                        if (force && config.getInfo().getFetchTimeMillis() == previousFetchTime) {
                            return Tasks.forException(new IllegalStateException("No fresh Remote Config response"));
                        }
                        return config.activate();
                    });
        }).addOnCompleteListener(DIRECT, result -> {
            if (result.isSuccessful()) {
                publish(callback);
            } else {
                callback.onSettings(null, null);
            }
        });
    }

    private void publish(FirebaseRemoteConfigCallback callback) {
        FirebaseRemoteConfigValue revokedInviteIds = config.getValue(REVOKED_INVITE_IDS);
        callback.onSettings(config.getString(SURVEY_URL),
                revokedInviteIds.getSource() == FirebaseRemoteConfig.VALUE_SOURCE_REMOTE ? revokedInviteIds.asString() : null);
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
                        callback.onSettings(null, null);
                    }
                });
            }
        }

        @Override
        public void onError(FirebaseRemoteConfigException error) {}
    }
}
