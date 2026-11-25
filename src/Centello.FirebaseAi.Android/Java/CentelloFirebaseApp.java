package centello.firebaseai;

import android.content.Context;
import com.google.firebase.FirebaseApp;
import com.google.firebase.FirebaseOptions;

final class CentelloFirebaseApp {
    private static FirebaseApp app;

    private CentelloFirebaseApp() {}

    static synchronized FirebaseApp get(Context context, String applicationId, String apiKey,
            String projectId, String senderId) {
        if (app != null) {
            return app;
        }

        FirebaseOptions options = new FirebaseOptions.Builder()
                .setApplicationId(applicationId)
                .setApiKey(apiKey)
                .setProjectId(projectId)
                .setGcmSenderId(senderId)
                .build();
        app = FirebaseApp.initializeApp(context, options);
        return app;
    }
}
