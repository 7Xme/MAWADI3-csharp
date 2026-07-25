# Improvements & Features to Add - Mawadi3Print

> Full codebase analysis (25 July 2026)

---

## Critical Bugs to Fix

### 1. `promptFeedback` block check happens after `candidates` access
**File:** `Services/ApiService.cs:116-131`

The code accesses `candidates[0]` **before** checking if the content was blocked via `promptFeedback`. If Gemini blocked the content, `candidates` may be empty, causing `IndexOutOfRangeException` before the block reason is ever examined.

**Fix:** Move the `promptFeedback` / `blockReason` check **before** accessing `candidates`.

---

### 2. `SnackbarMessage` is never reset / snackbar never auto-dismisses
**File:** `ViewModels/MainViewModel.cs:228-229`

`IsSnackbarVisible` is set to `true` but never set back to `false`. The snackbar stays open forever until another snackbar replaces it.

**Fix:** Add a timer (`DispatcherTimer` or `Task.Delay`) that hides the snackbar after 3-4 seconds.

---

### 3. `CancelGenerationCommand` is never wired in the UI
**File:** `ViewModels/MainViewModel.cs:238` | `MainWindow.xaml`

The ViewModel defines `CancelGenerationCommand` but no button or UI element binds to it. Users cannot cancel a running generation.

**Fix:** Add a cancel button visible during loading state in the XAML.

---

### 4. Word count range inconsistency
**Files:** `Services/ApiService.cs:31` | `ViewModels/MainViewModel.cs:126-130`

- Prompt asks for 350-400 words
- Code checks 350-420 as the "good" range
- Warning message says "المفروض مابين 350 و 400"

**Fix:** Make all three consistent (either accept 350-420 everywhere or 350-400 everywhere).

---

### 5. `CancellationTokenSource` leak
**File:** `ViewModels/MainViewModel.cs:108-109`

Each generation cancels the old `_cts` but never disposes it. `CancellationTokenSource` implements `IDisposable` and holds internal timer handles.

**Fix:** Call `_cts?.Dispose()` before creating a new one.

---

### 6. `StringContent` reuse across retries
**File:** `Services/ApiService.cs:54,59-92`

The same `StringContent` object is reused across retry attempts. After the first `PostAsync`, the content stream may already be consumed.

**Fix:** Create a new `StringContent` inside the retry loop for each attempt.

---

## Security Improvements

### 7. API key stored in plaintext
**File:** `Services/StorageService.cs:29`

The Gemini API key is saved as plain text in `%LocalAppData%\Mawadi3Print\settings.json`. Any process running as the same user can read it.

**Fix:** Use `System.Security.Cryptography.ProtectedData` (DPAPI) or Windows Credential Manager to encrypt the key at rest.

---

### 8. API key passed as URL query parameter
**File:** `Services/ApiService.cs:67`

The key is appended to the URL as `?key={apiKey}`. It will appear in logs, crash dumps, and proxy history.

**Fix:** Send the key via the `x-goog-api-key` HTTP header instead.

---

### 9. No model name validation
**File:** `Services/ApiService.cs:102-109`

`SanitizeModel` only strips a `models/` prefix. It does not validate against an allowlist. A corrupted settings file could inject an arbitrary model name into the API URL.

**Fix:** Validate `model` against the known `Models` list in the ViewModel before passing it to the API.

---

### 10. Prompt injection via topic input
**File:** `Services/ApiService.cs:31`

The user-provided topic is embedded directly into the API prompt. A crafted topic string could manipulate the AI's behavior.

**Fix:** Sanitize the topic input or use structured prompt templates that separate instructions from user data.

---

## Missing Features to Add

### 11. Article history / local storage
**Priority: HIGH**

Generated articles exist only in memory. Closing the app or generating a new article loses the previous one permanently.

**Add:**
- SQLite database or local JSON file to store generated articles
- A history panel/dialog showing past articles
- Ability to reopen, edit, re-print, or delete saved articles

---

### 12. Dark mode toggle
**File:** `App.xaml:9`

`BaseTheme="Light"` is hardcoded. MaterialDesignThemes supports dark mode natively.

**Add:**
- A toggle button in the app bar
- Persist the user's theme preference in settings
- Use `materialDesign:BundledTheme` with dynamic `BaseTheme`

---

### 13. Custom word count target
**File:** `Services/ApiService.cs:31` | `ViewModels/MainViewModel.cs`

The 350-400 word range is hardcoded. Users may want shorter or longer articles.

**Add:**
- A slider or dropdown to select desired word count (e.g., 200, 300, 400, 500, 800)
- Pass the target to the API prompt dynamically

---

### 14. Additional language support
**File:** `Services/ApiService.cs:29` | `ViewModels/MainViewModel.cs`

Only French and Arabic are supported. The app could serve a wider audience.

**Add:**
- English, Spanish, or other languages
- Dynamic language list in the ViewModel
- Proper prompt generation for each language

---

### 15. Temperature / generation parameter controls
**File:** `Services/ApiService.cs:48-49`

Temperature (`0.3`) and max tokens (`1500`) are hardcoded.

**Add:**
- An "Advanced settings" section with temperature slider (0.0 - 1.0)
- Max tokens / output length control

---

### 16. Save as plain text (.txt) option
**File:** `ViewModels/MainViewModel.cs`

Users can only copy to clipboard, print, or save as PDF.

**Add:**
- "Save as TXT" button alongside the PDF button
- Simple `File.WriteAllTextAsync` implementation

---

### 17. Regenerate / retry button
**File:** `ViewModels/MainViewModel.cs`

No dedicated retry command exists. Users must re-click generate manually.

**Add:**
- A "Regenerate" button that re-runs the last generation with the same topic and settings
- Visual distinction from the initial generate button

---

### 18. Keyboard shortcuts
**File:** `MainWindow.xaml`

No keyboard shortcuts exist.

**Add:**
- `Ctrl+Enter` to generate article
- `Ctrl+C` to copy article
- `Ctrl+P` to print
- `Ctrl+S` to save as PDF
- `Escape` to cancel generation

---

### 19. Confirmation dialog before clearing API key
**File:** `ViewModels/MainViewModel.cs:319`

`ClearKeyCommand` immediately removes the key with no confirmation.

**Add:**
- A `MaterialDesign` confirmation dialog before clearing

---

### 20. Export as Word (.docx) format
**Priority: MEDIUM**

PDF is useful for printing, but many users need editable documents.

**Add:**
- Use `DocumentFormat.OpenXml` NuGet package
- Generate a `.docx` file with proper RTL support and formatting

---

## UX/UI Improvements

### 21. Snackbar auto-dismiss timer
See bug #2 above. Additionally:
- Show a subtle close button on the snackbar
- Different colors for success vs error vs info messages

---

### 22. Visual validation on topic input
**File:** `MainWindow.xaml:116`

No visual feedback until the user clicks Generate.

**Add:**
- Red border or helper text when the topic field is empty
- Clear validation state when the user starts typing

---

### 23. Font size / zoom control for article preview
**File:** `MainWindow.xaml:261-296`

Preview uses fixed font sizes. Users who need larger text have no option.

**Add:**
- A zoom slider above the preview panel
- Bind `FontSize` of the preview `TextBlock` to a ViewModel property

---

### 24. Loading state for API key verification on startup
**File:** `MainWindow.xaml.cs:17-35`

No loading indicator during `InitializeAsync()`. The user sees nothing.

**Add:**
- A splash/loading overlay while the key check runs

---

### 25. Remove redundant `DialogHost` in `SettingsDialog.xaml`
**File:** `Views/SettingsDialog.xaml:17`

The settings dialog wraps content in its own `DialogHost Identifier="SettingsDialog"`, but it is opened via the parent's `"RootDialog"`. The inner `DialogHost` is unnecessary nesting.

**Fix:** Remove the inner `DialogHost` wrapper.

---

### 26. Show visual feedback when hyperlink URL is copied
**File:** `MainWindow.xaml.cs:42` | `Views/SettingsDialog.xaml.cs:19`

Clicking a hyperlink silently copies the URL to clipboard with no feedback.

**Fix:** Show a brief snackbar message like "تم النسخ" (Copied).

---

## Architecture & Code Quality

### 27. Separate `SettingsViewModel` into its own file
**File:** `ViewModels/MainViewModel.cs:259-341`

`SettingsViewModel` is a 80-line class sharing the ViewModel file with `MainViewModel`.

**Fix:** Create `ViewModels/SettingsViewModel.cs` and move it there.

---

### 28. Move `ComboItem` to `Models/`
**File:** `ViewModels/MainViewModel.cs:245-257`

`ComboItem` is a data class that belongs in `Models/ComboItem.cs`.

---

### 29. Add interfaces for all services
**Files:** `Services/ApiService.cs`, `Services/StorageService.cs`, `Services/PrintService.cs`

No interfaces exist. Unit testing and mocking are impossible.

**Add:**
- `IApiService`, `IStorageService`, `IPrintService` interfaces
- Constructor injection in ViewModels

---

### 30. Add dependency injection
**File:** `ViewModels/MainViewModel.cs:13-14`

Services are created with `new` in the ViewModel. No DI container is used.

**Add:**
- Use `Microsoft.Extensions.DependencyInjection`
- Register services and ViewModels in `App.xaml.cs`
- Use `IServiceProvider` to resolve dependencies

---

### 31. Make `PrintService` non-static
**File:** `Services/PrintService.cs:14`

All methods are static, blocking injection and testing.

**Fix:** Convert to an instance class implementing `IPrintService`.

---

### 32. Move QuestPDF license setup to app startup
**File:** `Services/PrintService.cs:119`

`QuestPDF.Settings.License = LicenseType.Community` is called on every PDF save.

**Fix:** Set it once in `App.OnStartup` or in a static constructor.

---

### 33. Add typed request/response DTOs for Gemini API
**File:** `Services/ApiService.cs:33-51`

The API request body uses anonymous types and the response is parsed manually with `JsonDocument`.

**Add:**
- `GeminiRequest`, `GeminiResponse`, `GeminiCandidate`, `GeminiPart` classes
- Use `System.Text.Json` deserialization for the response

---

### 34. Add null-safety to `ParseResponse`
**File:** `Services/ApiService.cs:116-121`

Deeply nested JSON access (`candidates[0].content.parts[0].text`) has no null checks. Missing properties cause `KeyNotFoundException`.

**Fix:** Use `TryGetProperty` and validate the response structure before accessing nested values.

---

### 35. Make `Article` model immutable
**File:** `Models/Article.cs`

All properties have public setters despite being a data transfer object.

**Fix:** Use `init` setters or convert to a `record` type.

---

### 36. Add `IDisposable` to `ApiService`
**File:** `Services/ApiService.cs:11`

`HttpClient` is never disposed.

**Fix:** Implement `IDisposable` on `ApiService` and dispose `_http`. Or use `IHttpClientFactory`.

---

## Performance Improvements

### 37. Use `IHttpClientFactory` or singleton `HttpClient`
**File:** `Services/ApiService.cs:11`

A new `HttpClient` is created per ViewModel instance.

**Fix:** Use a singleton `HttpClient` or `IHttpClientFactory` to share connections.

---

### 38. Add retry logic for transient network errors
**File:** `Services/ApiService.cs:59-92`

The retry loop only retries for low word count. It does not handle 503, 502, or network timeouts.

**Fix:** Add retry with exponential backoff for transient HTTP errors (5xx, timeouts).

---

### 39. Cancel old `CancellationTokenSource` before disposal
See bug #5 above.

---

## Localization & Internationalization

### 40. Create `.resx` resource files
**File:** All `.cs` and `.xaml` files

All strings are hardcoded in Arabic. The `.csproj` declares `SatelliteResourceLanguages=ar;fr` but no resources exist.

**Add:**
- `Resources/Strings.resx` (default/Arabic)
- `Resources/Strings.fr.resx` (French)
- Replace all hardcoded strings with `{x:Static resources:Strings.StringName}` or `resources:Strings.StringName`

---

### 41. Standardize the Arabic dialect
**Files:** All source files

The UI mixes Moroccan Darija ("كايْتولد", "ماكاينش") with Modern Standard Arabic ("الرجاء إدخال الموضوع").

**Fix:** Pick one dialect and use it consistently throughout.

---

### 42. Dynamic RTL/LTR for the entire UI
**Files:** `MainWindow.xaml:16`, `Views/SettingsDialog.xaml:12`

`FlowDirection` is hardcoded to `RightToLeft`. If French UI is added, this must change dynamically.

**Fix:** Bind `FlowDirection` to a converter based on the selected language.

---

## Testing

### 43. Add a test project
**Solution level**

No test project exists at all.

**Add:**
- `Mawadi3Print.Tests` project with xUnit or NUnit
- Unit tests for `ApiService`, `StorageService`, `MainViewModel`
- Mock `IApiService` and `IStorageService` for ViewModel tests

---

### 44. Add unit tests for `ParseResponse` edge cases
**File:** `Services/ApiService.cs:111-148`

The response parser handles Gemini's JSON output. It should be tested with:
- Valid responses
- Empty `candidates` array
- Blocked content
- Missing properties
- Unexpected JSON structure

---

### 45. Add unit tests for `ParseColor` in converters
**File:** `Converters/Converters.cs:25-34`

The hex color parser only handles 6-char hex. It should be tested with:
- Valid 6-char hex (`#FF0000`)
- Named colors (`Green`, `Orange`)
- 3-char hex (`#F00`)
- 8-char hex with alpha (`#FFFF0000`)
- Invalid input (`""`, `"0r"`, `"hello"`)

---

## Summary

| Category | Count |
|----------|-------|
| Critical Bugs | 6 |
| Security | 4 |
| Missing Features | 10 |
| UX/UI | 6 |
| Architecture | 10 |
| Performance | 3 |
| Localization | 3 |
| Testing | 3 |
| **Total** | **45** |
