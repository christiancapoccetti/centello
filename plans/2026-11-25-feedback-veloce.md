# Quick feedback in Settings

Frame complete. Small addition to the existing Information section. Design approved in mockups/2026-11-25-feedback-veloce.html. Architecture ready for approval. Implementation, review, verification, documentation and hand-off pending.

## Change

Read the activated string named `feedback_form_url` from Firebase Remote Config through FirebaseRemoteConfigBridge. Reuse its configured client, ordinary fetch interval and activation policy. Make the last activated value available on opening Settings and refresh asynchronously, including on a first install. The setting must work independently of AI consent, invites, survey URLs and successful AI settings parsing. Keep its default empty. A failed fetch retains the last activated value. A successful removal hides the row.

Expose an optional validated absolute HTTPS URI to SettingsViewModel. Use it for both visibility and navigation. Insert the approved SettingsRow above Version in SettingsPage.xaml, with the chat glyph, external-link indicator and Italian copy from the mockup. Open the configured destination through the existing Android launcher pattern. Handle launch failure with the existing snackbar. No in-app form, submission endpoint or hard-coded destination.

## Checks and delivery

1. Cover valid, absent, removed and malformed values, and feedback configured without AI parameters.
2. Build the Android target and exercise Settings with a controlled configuration on the closest available test device. Verify placement, external navigation and hidden state. Leave production configuration untouched.
3. Review the diff, run the repository test suite, and document the configuration key alongside the existing Remote Config settings.
