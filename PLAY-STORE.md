# Google Play release notes

## App identity

- **Store name:** Playroom — Mini Games
- **Android application ID:** `com.gamemania.playroom`
- **Category:** Games
- **Suggested tags:** Casual, Puzzle
- **Short description (66/80 characters):** Five thoughtful little games for quick breaks and feel-good focus.

## Full description

Make a little room for play. Playroom brings five familiar games together in one calm, cream-colored collection—ready whenever you need a small break.

• Connect Four — take turns and line up four.
• Rock Paper Scissors — pick your move and see what happens.
• Memory Match — find all eight pairs at your own pace.
• 2048 — slide and merge tiles to reach a bigger number.
• Whack-a-Mole — catch as many little moles as you can in a quick round.

No account is needed. Your game records stay on your device, and every game is available offline.

## Before submitting

1. Build and test a **signed Android App Bundle (AAB)** on real Android devices.
2. Upload `assets/icon-512.png` as the high-resolution app icon, `assets/feature-graphic.png` as the feature graphic, and add phone screenshots captured from the app in Play Console's current required sizes.
3. Host `privacy-policy.html` at a public HTTPS URL and enter that URL in Play Console. Add a monitored support contact to the Play listing.
4. Complete Play Console's Data safety, target audience, ads, content rating, app access, and other declarations accurately. This app currently has no ads, analytics, sign-in, or network-backed game features.
5. Set the target SDK required by Google Play at submission time, then test on supported screen sizes and Android versions.
6. Confirm the application ID is controlled by you and hasn't already been registered. It is an app's permanent identity after publishing.

The Play Store app uses native Android views; it does not display the website or use a WebView. The separate web edition can still be rebuilt with `npm run build`. Open the `android` folder directly in Android Studio to build and sign the Android App Bundle. Signing keys, a Play Console account, the public policy URL, phone screenshots, and submission declarations must be supplied during release preparation.
