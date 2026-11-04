# S3 Windows appearance verification

Verified 2026-11-04. UI-01..04 pass; S3 is COMPLETE. S4 owns final performance,
packaging and delivery. No product release or installed-system change occurred.

The actual WPF interface uses neutral light/dark surfaces, Segoe UI, 30 DIP rows,
32/34 DIP commands and native Windows caption/resize behavior. Appearance settings
provide persisted System/Light/Dark and system/custom accent choices with inline
invalid-color feedback. Shared mutable palette resources update owned views,
menus and release notes without restarting the scan. High contrast takes Windows
system colors and disables app overrides; focus has a visible 2 DIP outline.
Contrast checks cover very light/dark and saturated accents in both themes.

The owned headless Sandbox exercised real WPF controls and Settings handlers,
expanded live folders, all retained views, menus, stopped/restarted/completed,
empty and actual guest ACL-denied scans. Exact controlled totals, selection and
thread exit passed. A fresh process loaded persisted Dark/custom accent settings.
The existing native classic regression also passed: 2,100-file virtualized
listing, keyboard selection, scroll preservation, rescan/history and close.

Actual HWND setting/theme messages traversed the production notification path,
with an isolated preference-source override; generation, selection and active
scan were preserved. A separate read-only host check matched the actual Windows
app preference, accent and high-contrast source. No host OS preference changed.

Layout evidence measures client bounds at 960x560, 1280x820 and 1600x1000 DIPs.
Controlled WM_DPICHANGED messages produced real WPF transforms 1.0, 1.5 and 2.0
and 96/144/192 DPI native visual-tree raster exports. The minimum client measured
964x561 at 100% and 972x580.5 at 200%; primary/wide bounds matched their requests.
The headless desktop was 1024x768, so a test-only minimum-size fixture supplied
the actual larger HWND bounds; these are measured native layouts, not physical
monitor screenshots. A guest display-mode request returned -1 and changed nothing;
it is not acceptance evidence. Product minimum sizing is unchanged by the fixture.
Physical monitor switching and DWM material rendering are not claimed. Composition
was unavailable in the guest and coherent solid-surface fallback passed.

Coordinator ticket `6ff9477a245541589118caa495f7c7a3` owns the exact guest
`6ff9477a-2455-4158-9118-caa495f7c7a3`. The final native request
`s3-focus-outline-final-acceptance` exited 0 with passed structured evidence.
The aggregate workflow records exit 1 from earlier diagnostic failures; it is
not reported as a green aggregate run. Current status independently confirms
the guest exited and its lease was released. No host desktop fallback occurred.

The final normal 0.0.3 build passed CLI/baseline CSV parity, HTML and independent
identity/configuration checks. Production builds omit UI actions, replay hooks
and the appearance-source override. SHA-256:
`baa4e4eb41f645820abd76252a31f49dbb3d69a3af8f35ed76bcdecab5b0b99f`.
Native captures were taken immediately before the metadata-only 0.0.3 bump;
appearance implementation, resources and manifest match the verified normal build.
Authenticode status remains NotSigned; no trust policy changed.

Ignored `.local/s3/` retains selected actual native captures, structured results
and source hashes for S4. `.local/development/` holds the matching internal build
and runtime DPI configuration. Private Explorer reference/account preferences
remain outside version control. Exact task scratch is transactionally cleaned
after retention and owned guest exit; its receipt is recorded in the plan.

Closure assessment: one appearance owner and shared resources serve existing WPF
views; no new framework, compositor workaround or competing state owner is needed.
No S3 blocker or tracked deferral remains.
