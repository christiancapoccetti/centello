package centello.firebaseai;

public interface FirebaseRemoteConfigCallback {
    void onSettings(String surveyUrl, String revokedInviteIds);
}
