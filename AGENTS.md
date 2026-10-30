# Diskweft development

Read `docs/SPECIFICATION.md` first. It is authoritative. Maintain stage status
and requirement reconciliation in `docs/IMPLEMENTATION-PLAN.md`.

- The user authorized S1 on 2026-10-30. S0 is COMPLETE and S1 alone is ACTIVE;
  S2-S5 remain NOT STARTED. Finish and publish S1 source/evidence without an
  intermediate product release.
- Keep at most one ACTIVE stage. Validate complete specified workflows rather
  than treating individual components or passing tests as delivered features.
- Preserve upstream history and MIT attribution. `origin` is Darkaxt/Diskweft;
  `upstream` is limburatorul/spacescan. Default GitHub operations target the fork.
- S0 compiles the unchanged upstream app and invokes scanner methods by reflection
  against generated fixtures. Do not launch the inherited GUI or installer.
- Isolate executable, settings/history, registry, ecosystem card, installer,
  shell and updater identities in S1 before ordinary-user-state application trials.
- Keep WPF and the existing scanner engines. The UI owns its displayed state;
  scanner-owned mutable collections must not be bound directly across threads.
- Windows 11 and the supplied Explorer reference govern appearance. Use Huashu
  guidance for scan-specific presentation where native conventions do not apply;
  it does not add an approval checkpoint or override the requested design.
- Do not change Pathweft. Its active checkout is reference context, not an
  authorized editing target or a source of unverified shared implementation.
- Use the installed execution/cleanup gates required by the user's standing
  instructions. Do not bypass administrator consent for MFT verification.
- `.local/` is ignored private evidence and retained baseline assets. Generated
  expendable fixtures use exact task scratch and transactional cleanup.
- No intermediate product releases. S4 owns the final verified Diskweft release.
- S5 owns the later requested system replacement of installed SpaceScan, including
  actual consumer inventory, preserved settings/history, route changes and rollback.
