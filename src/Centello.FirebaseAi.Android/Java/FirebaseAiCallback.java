package centello.firebaseai;

public interface FirebaseAiCallback {
    void onResult(String json, int inputTokens, int outputTokens);
    void onError(String status);
}
