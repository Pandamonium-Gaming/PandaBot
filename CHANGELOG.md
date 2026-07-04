# Changelog

All notable changes to PandaBot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.9.1] - 2026-07-04

### Added

* add /warnings clear with interactive removal picker ([`badfec1`])
* add ClearWarningsAsync and RemoveWarningsAsync to WarningService ([`3f4d180`])
* add all-caps auto-moderation ([`3336a64`])
* reuse one forum thread per user instead of creating a new one per action ([`b31c229`])
* add durable UserWarning table backing /warn strikes ([`9666e94`])
* link member name to Discord profile in join embed ([`f8ccfc0`])
* add member join logging and per-user ignore list; bump version to 1.7.0 ([`a312bdd`])
* implement move messages and thread relocation commands ([`6fda099`])
* add event audit logging and deploy secret wiring ([`0867f28`])
* show offending image in cross-channel spam log embed ([`81f8266`])
* show username and id in list output ([`efa57b5`])
* add persistent background history backfill with resume capability ([`0448781`])
* update changelog for version 1.6.8; fix single-message enable interaction timeout and update version number ([`3d52a09`])
* update changelog for version 1.6.7; add configurable cross-channel spam enforcement options (delete messages and timeout) ([`70c8694`])
* update changelog for version 1.6.6; implement optional content for live test and enhance detection handling ([`2006eca`])
* update changelog for version 1.6.5; add attachment support to spam live test ([`c37f826`])
* update changelog for version 1.6.4; enhance cross-channel spam detection and add live test command ([`1c6d25a`])
* add command access controls and moderation exemptions to deployment configuration; update README ([`5f70da2`])
* add moderation exemptions and command access controls; update configuration and services ([`73317d0`])
* implement cross-channel spam detection module and enhance single-message enforcement ([`c12cbad`])
* add moderation log and cross-channel spam configuration to .env file ([`b06a27d`])
* add ModerationLogService, CrossChannelSpamDetector, and moderation action logging ([`e1b422f`])
* add single-message channel enforcement feature and update documentation ([`ddf64fe`])
* wire SingleMessage commands, service and tests for PandaBot ([`c9ff323`])
* add SingleMessageService for PandaBot ([`0328bc7`])
* add SingleMessage EF migration for PandaBot ([`73fd4e7`])
* add SingleMessage model classes for PandaBot ([`04b79b9`])
* enhance deployment process with staging directory for service and journald config files ([`f959e06`])
* add Prometheus metrics endpoint and update version to 1.5.37 ([`c2b5907`])
* add GUILD_ID environment variable to .env file and update documentation ([`00fd422`])
* update version to 1.5.36 and enhance HTTP request handling in HeartbeatMonitorService ([`c10e26d`])
* update version to 1.5.35 and downgrade disconnect lifecycle warnings to informational logging ([`59ac90e`])
* update version to 1.5.34 and enhance slash command registration handling ([`87107d5`])
* update version to 1.5.33 and implement single registration for slash commands ([`5e38f4a`])
* add deployment workflow for Digital Ocean and update README references ([`fb82e9a`])
* add Discord lifecycle observability with connection and disconnection logging ([`c48ade2`])
* add validation for BotConfig and HeartbeatConfig; update version to 1.5.6 ([`c391494`])
* enhance Discord configuration binding with error handling and diagnostics ([`4b78760`])
* wire heartbeat deploy secrets and enhance service health checks; bump version to 1.5.4 ([`a779b06`])
* implement configurable uptime heartbeat monitor and related services ([`8458bcd`])
* add VersionManager tests and update changelog handling ([`ab0cf73`])
* add new custom words for improved spell checking ([`61c24ea`])
* enhance sunrise and sunset display with real-world time conversion ([`b50c287`])
* add sunrise and sunset calculations to location time info ([`58df44b`])
* add in-game date properties and enhance date calculation in VerseTimeService ([`0d15621`])
* enhance location selection interaction and improve fuzzy matching logic ([`3241a7d`])
* update version to 1.4.2, enhance location selection with dropdown, and improve fuzzy matching logic ([`066bece`])
* update AI instructions and changelog for clarity and consistency ([`ca39f08`])
* update version to 1.4.1 and enhance fuzzy location search algorithm ([`1998740`])
* add /sc time command for local time retrieval and implement VerseTime service ([`2ca0e28`])
* implement automatic cache versioning for vehicle cache ([`e173e23`])
* use full terminal name in location display ([`070e9d6`])
* enhance commodity location display with full system hierarchy ([`bd61db9`])
* display full location names instead of terminal codes ([`2968d16`])
* display top 5 prices for buy/sell/purchase/rental ([`b3527ba`])
* auto-populate UEX vehicle cache on startup ([`fe68f2f`])
* Add vehicle purchase and rental price models with fetching methods ([`b58abe9`])
* Enhance vehicle pricing model with additional sale properties and currency information ([`ef17357`])
* Enhance vehicle option descriptions and derive vehicle types from API response ([`8d2c717`])
* Add UEX vehicle caching functionality ([`9c559bd`])
* Enhance UEX item caching by adding category fetching and updating response handling ([`3136ef8`])
* Add UEXItemCacheInitializerService to initialize item cache on startup ([`827bd99`])
* Update UEXCommodityService to use new API endpoints and improve data handling ([`149ef97`])
* Integrate UEX API configuration and update services for commodity pricing ([`121b3e6`])
* Add UEX commodity service and command for fetching commodity prices ([`4da607e`])
* Implement git-aware version tracking and add check-commits command for version validation ([`5c17bd7`])
* Implement automatic build validation for version consistency and update version to 1.2.3 ([`469deb3`])
* Add Discord.Commands namespace and update group attributes for Path of Exile, Return of Reckoning, and Star Citizen modules ([`3e98f0d`])
* Update to use ROR API for player count and status retrieval docs: Enhance GitHub Copilot instructions with PowerShell command guidelines docs: Update changelog for version 1.2.2 with ROR API integration details ([`1fbee15`])
* Update bot description and add game modules configuration section ([`05268e5`])
* Enhance `/about` command with version, module, and command counts ([`80e6df3`])
* Enhance logging during startup and service registration ([`c53127a`])
* Enhance module loading with timeout and detailed logging ([`efcc2e4`])
* Implement timeout for module loading and enhance logging during startup ([`7a25112`])
* Add GameModulesConfig for enabling/disabling game modules and update service registration ([`a44b9f8`])
* Add services and commands for Return of Reckoning server status ([`13bcf50`])
* Add new words to cspell configuration for improved spell checking ([`57163cb`])
* Add methods for profession level conversion in AshesRecipeService ([`89fb401`])
* Add initial migration and create tables for CachedItems, CachedMobs, CachedVendors, GuildSettings, CachedCraftingRecipes, MobItemDrops, CachedRecipeIngredients, and MobRecipeDrops ([`c263e0e`])
* Implement VersionManager for synchronizing .csproj and CHANGELOG versions ([`ad73cf8`])
* Add Path of Exile status command group ([`b9edc72`])
* Add .NET VersionManager tool for version synchronization ([`afc90a9`])
* Add automated version and changelog management ([`c1c383d`])
* Add Star Citizen status command using RSI status API ([`2ef49c5`])
* Add 'serverinfo' to custom words in cspell configuration ([`5ba93fd`])
* Regenerate database snapshot and update version to 1.0.1 ([`932df68`])
* Add bot version display in /serverinfo command and log version on startup ([`273f705`])
* Add CertificationLevel column to CachedCraftingRecipes migration ([`8a9a8d8`])
* Refactor deployment workflow and enhance JSON property extraction utilities ([`626f55b`])
* Add deployment setup guide and service configuration for PandaBot ([`ee7f439`])
* Enhance profession level handling and add recursive profession requirement fetching for crafting recipes ([`bf98032`])
* Enhance crafting and recipe services to display required skill levels and improve profession handling ([`9ae581d`])
* Enhance item and recipe services to calculate and display highest required skill levels for crafting ([`182225a`])
* Refactor profession level handling to improve readability and support certification levels from JSON ([`dcc86db`])
* Enhance level parsing logic to support tier and Roman numeral suffixes for crafting recipes ([`815d13d`])
* Enhance recipe processing logic to skip purification recipes and handle circular references in ingredients ([`8014f71`])
* Update recipe fetching logic to return raw JSON and enhance error handling ([`c350b6d`])
* Add API integration for fetching item recipes and enhance logging for recipe retrieval ([`7db454c`])
* Improve caching and enrichment logic in AshesForgeApiService and AshesRecipeService ([`b633f93`])
* Enhance recipe command with improved logging and background enrichment for recipes ([`2b48484`])
* Enhance AshesForgeApiService and AshesRecipeService with improved logging and caching for item and recipe details ([`6720c10`])
* Add Ashes of Creation module with item and recipe commands ([`30e77ab`])
* Implement AshesForge data caching services and item management ([`3675ffc`])

### Fixed

* log and surface component-command failures, run MessageReceived handlers off the gateway task ([`2c8e5f8`])
* fix mod-log thread lookup limit, /warnings ordering, and missing permission ([`0037eb3`])
* pin SQLitePCLRaw.bundle_e_sqlite3 to 3.0.3 to resolve CVE-2025-6965 ([`9e5a466`])
* enable service on boot via systemctl enable --now ([`176688d`])
* show username and profile image in member join/leave embeds ([`c671e99`])
* download image before deletion, delete follow-on messages from confirmed spammers ([`d65805f`])
* suppress delete log for bot and activity users in all resolution paths ([`b1ad84d`])
* store message snapshots for ignored users to enable correct delete suppression ([`08ad033`])
* restore deleted author mention and avatar icon ([`459c60e`])
* guarantee deleted-message avatar fallback ([`0733646`])
* stabilize deleted author name and avatar attribution ([`62c0178`])
* add receive-time fallback for deleted author attribution ([`5f0aca6`])
* enable message cache for deleted author capture ([`9e68008`])
* improve deleted message author attribution from audit logs ([`acec6e4`])
* add singlemessage backfill migration ([`70e31e4`])
* resolve startup hang in DiscordBotService by ensuring _readyCompletionSource is set in finally block ([`d487daa`])
* update deployment process to use staging directory for service files and journald configuration ([`35ade35`])
* handle null exception in DisconnectedAsync logging ([`27a2920`])
* enhance SSH key handling in deployment workflow ([`a32e017`])
* enhance suppression of gateway log noise during reconnects ([`000214e`])
* suppress empty messages from Discord gateway during reconnects ([`2dbf3ac`])
* suppress empty gateway reconnect noise ([`5967587`])
* reduce false downtime during redeploy ([`f557a78`])
* enhance category processing with detailed logging and error tracking ([`69ee6d5`])
* treat data-null category payloads as empty ([`98242fc`])
* reduce noise for empty category item responses ([`13f5dd0`])
* query UEX items by category to satisfy API requirements ([`70abbd4`])
* add bearer token auth and configurable base URL to UEXVehicleService ([`f6b42e6`])
* add User-Agent header to all Star Citizen API HTTP clients ([`e456b7e`])
* add UEX cache auto-refresh and item cache initialization ([`425a580`])
* correct formatting for location search fuzzy matching entry ([`c9d05c0`])
* clarify commodity price labels as buy/sell prices ([`00dbcb1`])
* filter zero prices in commodity embed display ([`79f4808`])
* extract manufacturer from company_name field in UEX API ([`294947d`])
* Refactor location formatting logic for improved clarity and accuracy ([`365370c`])
* Update dropdown component handling to delete original response message after selection ([`bce5fd7`])
* Add logging for first price data and enrich location information in vehicle pricing ([`fef00b9`])
* Remove dropdown components from original response in vehicle and item commands ([`99bc12d`])
* Enhance vehicle data handling and improve price display in UEX vehicle service ([`64db563`])
* Update item pricing retrieval to use item ID instead of name for improved accuracy ([`43dbbc1`])
* Update item selection handling to ensure user-specific interactions and improve error feedback ([`1e572c4`])
* Update FollowupAsync calls to set ephemeral parameter for improved user feedback ([`67c1796`])
* Update caching logic to use expiration cutoff for item retrieval ([`249d7b1`])
* Correct repository root path and enhance cross-platform compatibility for version validation ([`a0ca99f`])
* Enhance error handling and logging during module loading ([`1c84902`])
* Update constructor to accept HttpClient directly instead of IHttpClientFactory ([`2ede280`])
* Refactor GetProfessionLevel method to use JsonHelper for property extraction ([`fa142fb`])
* Allow VersionManager source in git while ignoring build artifacts ([`a4d9d9d`])
* Update Path of Exile service to use correct API endpoints ([`6c2cb74`])
* Correct pre-commit hook regex for Windows and update setup docs ([`183b7b2`])
* Use RSI status index.json endpoint instead of blocked API endpoint ([`04a4ed3`])
* Add User-Agent header to Star Citizen status API requests ([`0775de3`])
* Update version to 1.0.3 in project file ([`88e3073`])
* Update version to 1.0.2 and adjust migration model snapshot for table definitions ([`f75d186`])
* Update deployment script to move .env file with sudo and improve security ([`e70606f`])
* Update deployment workflow to include code checkout and service file deployment ([`527f04a`])
* Update user in pandabot.service from 'deployment' to 'pandabot' ([`d06469f`])
* Remove defaultValueSql from CertificationLevel column in CachedCraftingRecipes migration ([`8100ba4`])
* Update default value for CertificationLevel column in CachedCraftingRecipes migration ([`0fefe92`])
* Update workflow triggers and enhance README with build status badge ([`c776e74`])
* Add database migration logic to the application startup process ([`21ffaab`])
* Improve deployment process by refining rsync commands for file transfer and cleanup ([`4de402d`])
* Optimize deployment process by simplifying rsync commands for file transfer ([`bf3224e`])
* Enhance deployment process with rsync for efficient file transfer and cleanup ([`9f1a6c6`])
* Correct deployment user in workflow and enhance .env file creation ([`355a4b8`])
* Update project name and deployment paths in workflow and solution files ([`bc30cfe`])
* Resolve merge conflict in Directory.Packages.props and update package versions ([`3095d5f`])

### Changed

* simplify workflow by removing deployment steps and renaming to .NET CI ([`4dab53c`])
* Replace ModifyOriginalResponseAsync with FollowupAsync for improved response handling ([`051b3f9`])
* Clean up project file by removing unused package references and folders ([`c18e05c`])

## [1.9.0] - 2026-07-04

### Added

* add /warnings clear with interactive removal picker ([`badfec1`])
* add ClearWarningsAsync and RemoveWarningsAsync to WarningService ([`3f4d180`])
* add all-caps auto-moderation ([`3336a64`])
* reuse one forum thread per user instead of creating a new one per action ([`b31c229`])
* add durable UserWarning table backing /warn strikes ([`9666e94`])
* link member name to Discord profile in join embed ([`f8ccfc0`])
* add member join logging and per-user ignore list; bump version to 1.7.0 ([`a312bdd`])
* implement move messages and thread relocation commands ([`6fda099`])
* add event audit logging and deploy secret wiring ([`0867f28`])
* show offending image in cross-channel spam log embed ([`81f8266`])
* show username and id in list output ([`efa57b5`])
* add persistent background history backfill with resume capability ([`0448781`])
* update changelog for version 1.6.8; fix single-message enable interaction timeout and update version number ([`3d52a09`])
* update changelog for version 1.6.7; add configurable cross-channel spam enforcement options (delete messages and timeout) ([`70c8694`])
* update changelog for version 1.6.6; implement optional content for live test and enhance detection handling ([`2006eca`])
* update changelog for version 1.6.5; add attachment support to spam live test ([`c37f826`])
* update changelog for version 1.6.4; enhance cross-channel spam detection and add live test command ([`1c6d25a`])
* add command access controls and moderation exemptions to deployment configuration; update README ([`5f70da2`])
* add moderation exemptions and command access controls; update configuration and services ([`73317d0`])
* implement cross-channel spam detection module and enhance single-message enforcement ([`c12cbad`])
* add moderation log and cross-channel spam configuration to .env file ([`b06a27d`])
* add ModerationLogService, CrossChannelSpamDetector, and moderation action logging ([`e1b422f`])
* add single-message channel enforcement feature and update documentation ([`ddf64fe`])
* wire SingleMessage commands, service and tests for PandaBot ([`c9ff323`])
* add SingleMessageService for PandaBot ([`0328bc7`])
* add SingleMessage EF migration for PandaBot ([`73fd4e7`])
* add SingleMessage model classes for PandaBot ([`04b79b9`])
* enhance deployment process with staging directory for service and journald config files ([`f959e06`])
* add Prometheus metrics endpoint and update version to 1.5.37 ([`c2b5907`])
* add GUILD_ID environment variable to .env file and update documentation ([`00fd422`])
* update version to 1.5.36 and enhance HTTP request handling in HeartbeatMonitorService ([`c10e26d`])
* update version to 1.5.35 and downgrade disconnect lifecycle warnings to informational logging ([`59ac90e`])
* update version to 1.5.34 and enhance slash command registration handling ([`87107d5`])
* update version to 1.5.33 and implement single registration for slash commands ([`5e38f4a`])
* add deployment workflow for Digital Ocean and update README references ([`fb82e9a`])
* add Discord lifecycle observability with connection and disconnection logging ([`c48ade2`])
* add validation for BotConfig and HeartbeatConfig; update version to 1.5.6 ([`c391494`])
* enhance Discord configuration binding with error handling and diagnostics ([`4b78760`])
* wire heartbeat deploy secrets and enhance service health checks; bump version to 1.5.4 ([`a779b06`])
* implement configurable uptime heartbeat monitor and related services ([`8458bcd`])
* add VersionManager tests and update changelog handling ([`ab0cf73`])
* add new custom words for improved spell checking ([`61c24ea`])
* enhance sunrise and sunset display with real-world time conversion ([`b50c287`])
* add sunrise and sunset calculations to location time info ([`58df44b`])
* add in-game date properties and enhance date calculation in VerseTimeService ([`0d15621`])
* enhance location selection interaction and improve fuzzy matching logic ([`3241a7d`])
* update version to 1.4.2, enhance location selection with dropdown, and improve fuzzy matching logic ([`066bece`])
* update AI instructions and changelog for clarity and consistency ([`ca39f08`])
* update version to 1.4.1 and enhance fuzzy location search algorithm ([`1998740`])
* add /sc time command for local time retrieval and implement VerseTime service ([`2ca0e28`])
* implement automatic cache versioning for vehicle cache ([`e173e23`])
* use full terminal name in location display ([`070e9d6`])
* enhance commodity location display with full system hierarchy ([`bd61db9`])
* display full location names instead of terminal codes ([`2968d16`])
* display top 5 prices for buy/sell/purchase/rental ([`b3527ba`])
* auto-populate UEX vehicle cache on startup ([`fe68f2f`])
* Add vehicle purchase and rental price models with fetching methods ([`b58abe9`])
* Enhance vehicle pricing model with additional sale properties and currency information ([`ef17357`])
* Enhance vehicle option descriptions and derive vehicle types from API response ([`8d2c717`])
* Add UEX vehicle caching functionality ([`9c559bd`])
* Enhance UEX item caching by adding category fetching and updating response handling ([`3136ef8`])
* Add UEXItemCacheInitializerService to initialize item cache on startup ([`827bd99`])
* Update UEXCommodityService to use new API endpoints and improve data handling ([`149ef97`])
* Integrate UEX API configuration and update services for commodity pricing ([`121b3e6`])
* Add UEX commodity service and command for fetching commodity prices ([`4da607e`])
* Implement git-aware version tracking and add check-commits command for version validation ([`5c17bd7`])
* Implement automatic build validation for version consistency and update version to 1.2.3 ([`469deb3`])
* Add Discord.Commands namespace and update group attributes for Path of Exile, Return of Reckoning, and Star Citizen modules ([`3e98f0d`])
* Update to use ROR API for player count and status retrieval docs: Enhance GitHub Copilot instructions with PowerShell command guidelines docs: Update changelog for version 1.2.2 with ROR API integration details ([`1fbee15`])
* Update bot description and add game modules configuration section ([`05268e5`])
* Enhance `/about` command with version, module, and command counts ([`80e6df3`])
* Enhance logging during startup and service registration ([`c53127a`])
* Enhance module loading with timeout and detailed logging ([`efcc2e4`])
* Implement timeout for module loading and enhance logging during startup ([`7a25112`])
* Add GameModulesConfig for enabling/disabling game modules and update service registration ([`a44b9f8`])
* Add services and commands for Return of Reckoning server status ([`13bcf50`])
* Add new words to cspell configuration for improved spell checking ([`57163cb`])
* Add methods for profession level conversion in AshesRecipeService ([`89fb401`])
* Add initial migration and create tables for CachedItems, CachedMobs, CachedVendors, GuildSettings, CachedCraftingRecipes, MobItemDrops, CachedRecipeIngredients, and MobRecipeDrops ([`c263e0e`])
* Implement VersionManager for synchronizing .csproj and CHANGELOG versions ([`ad73cf8`])
* Add Path of Exile status command group ([`b9edc72`])
* Add .NET VersionManager tool for version synchronization ([`afc90a9`])
* Add automated version and changelog management ([`c1c383d`])
* Add Star Citizen status command using RSI status API ([`2ef49c5`])
* Add 'serverinfo' to custom words in cspell configuration ([`5ba93fd`])
* Regenerate database snapshot and update version to 1.0.1 ([`932df68`])
* Add bot version display in /serverinfo command and log version on startup ([`273f705`])
* Add CertificationLevel column to CachedCraftingRecipes migration ([`8a9a8d8`])
* Refactor deployment workflow and enhance JSON property extraction utilities ([`626f55b`])
* Add deployment setup guide and service configuration for PandaBot ([`ee7f439`])
* Enhance profession level handling and add recursive profession requirement fetching for crafting recipes ([`bf98032`])
* Enhance crafting and recipe services to display required skill levels and improve profession handling ([`9ae581d`])
* Enhance item and recipe services to calculate and display highest required skill levels for crafting ([`182225a`])
* Refactor profession level handling to improve readability and support certification levels from JSON ([`dcc86db`])
* Enhance level parsing logic to support tier and Roman numeral suffixes for crafting recipes ([`815d13d`])
* Enhance recipe processing logic to skip purification recipes and handle circular references in ingredients ([`8014f71`])
* Update recipe fetching logic to return raw JSON and enhance error handling ([`c350b6d`])
* Add API integration for fetching item recipes and enhance logging for recipe retrieval ([`7db454c`])
* Improve caching and enrichment logic in AshesForgeApiService and AshesRecipeService ([`b633f93`])
* Enhance recipe command with improved logging and background enrichment for recipes ([`2b48484`])
* Enhance AshesForgeApiService and AshesRecipeService with improved logging and caching for item and recipe details ([`6720c10`])
* Add Ashes of Creation module with item and recipe commands ([`30e77ab`])
* Implement AshesForge data caching services and item management ([`3675ffc`])

### Fixed

* fix mod-log thread lookup limit, /warnings ordering, and missing permission ([`0037eb3`])
* pin SQLitePCLRaw.bundle_e_sqlite3 to 3.0.3 to resolve CVE-2025-6965 ([`9e5a466`])
* enable service on boot via systemctl enable --now ([`176688d`])
* show username and profile image in member join/leave embeds ([`c671e99`])
* download image before deletion, delete follow-on messages from confirmed spammers ([`d65805f`])
* suppress delete log for bot and activity users in all resolution paths ([`b1ad84d`])
* store message snapshots for ignored users to enable correct delete suppression ([`08ad033`])
* restore deleted author mention and avatar icon ([`459c60e`])
* guarantee deleted-message avatar fallback ([`0733646`])
* stabilize deleted author name and avatar attribution ([`62c0178`])
* add receive-time fallback for deleted author attribution ([`5f0aca6`])
* enable message cache for deleted author capture ([`9e68008`])
* improve deleted message author attribution from audit logs ([`acec6e4`])
* add singlemessage backfill migration ([`70e31e4`])
* resolve startup hang in DiscordBotService by ensuring _readyCompletionSource is set in finally block ([`d487daa`])
* update deployment process to use staging directory for service files and journald configuration ([`35ade35`])
* handle null exception in DisconnectedAsync logging ([`27a2920`])
* enhance SSH key handling in deployment workflow ([`a32e017`])
* enhance suppression of gateway log noise during reconnects ([`000214e`])
* suppress empty messages from Discord gateway during reconnects ([`2dbf3ac`])
* suppress empty gateway reconnect noise ([`5967587`])
* reduce false downtime during redeploy ([`f557a78`])
* enhance category processing with detailed logging and error tracking ([`69ee6d5`])
* treat data-null category payloads as empty ([`98242fc`])
* reduce noise for empty category item responses ([`13f5dd0`])
* query UEX items by category to satisfy API requirements ([`70abbd4`])
* add bearer token auth and configurable base URL to UEXVehicleService ([`f6b42e6`])
* add User-Agent header to all Star Citizen API HTTP clients ([`e456b7e`])
* add UEX cache auto-refresh and item cache initialization ([`425a580`])
* correct formatting for location search fuzzy matching entry ([`c9d05c0`])
* clarify commodity price labels as buy/sell prices ([`00dbcb1`])
* filter zero prices in commodity embed display ([`79f4808`])
* extract manufacturer from company_name field in UEX API ([`294947d`])
* Refactor location formatting logic for improved clarity and accuracy ([`365370c`])
* Update dropdown component handling to delete original response message after selection ([`bce5fd7`])
* Add logging for first price data and enrich location information in vehicle pricing ([`fef00b9`])
* Remove dropdown components from original response in vehicle and item commands ([`99bc12d`])
* Enhance vehicle data handling and improve price display in UEX vehicle service ([`64db563`])
* Update item pricing retrieval to use item ID instead of name for improved accuracy ([`43dbbc1`])
* Update item selection handling to ensure user-specific interactions and improve error feedback ([`1e572c4`])
* Update FollowupAsync calls to set ephemeral parameter for improved user feedback ([`67c1796`])
* Update caching logic to use expiration cutoff for item retrieval ([`249d7b1`])
* Correct repository root path and enhance cross-platform compatibility for version validation ([`a0ca99f`])
* Enhance error handling and logging during module loading ([`1c84902`])
* Update constructor to accept HttpClient directly instead of IHttpClientFactory ([`2ede280`])
* Refactor GetProfessionLevel method to use JsonHelper for property extraction ([`fa142fb`])
* Allow VersionManager source in git while ignoring build artifacts ([`a4d9d9d`])
* Update Path of Exile service to use correct API endpoints ([`6c2cb74`])
* Correct pre-commit hook regex for Windows and update setup docs ([`183b7b2`])
* Use RSI status index.json endpoint instead of blocked API endpoint ([`04a4ed3`])
* Add User-Agent header to Star Citizen status API requests ([`0775de3`])
* Update version to 1.0.3 in project file ([`88e3073`])
* Update version to 1.0.2 and adjust migration model snapshot for table definitions ([`f75d186`])
* Update deployment script to move .env file with sudo and improve security ([`e70606f`])
* Update deployment workflow to include code checkout and service file deployment ([`527f04a`])
* Update user in pandabot.service from 'deployment' to 'pandabot' ([`d06469f`])
* Remove defaultValueSql from CertificationLevel column in CachedCraftingRecipes migration ([`8100ba4`])
* Update default value for CertificationLevel column in CachedCraftingRecipes migration ([`0fefe92`])
* Update workflow triggers and enhance README with build status badge ([`c776e74`])
* Add database migration logic to the application startup process ([`21ffaab`])
* Improve deployment process by refining rsync commands for file transfer and cleanup ([`4de402d`])
* Optimize deployment process by simplifying rsync commands for file transfer ([`bf3224e`])
* Enhance deployment process with rsync for efficient file transfer and cleanup ([`9f1a6c6`])
* Correct deployment user in workflow and enhance .env file creation ([`355a4b8`])
* Update project name and deployment paths in workflow and solution files ([`bc30cfe`])
* Resolve merge conflict in Directory.Packages.props and update package versions ([`3095d5f`])

### Changed

* simplify workflow by removing deployment steps and renaming to .NET CI ([`4dab53c`])
* Replace ModifyOriginalResponseAsync with FollowupAsync for improved response handling ([`051b3f9`])
* Clean up project file by removing unused package references and folders ([`c18e05c`])

## [1.8.1] - 2026-07-04

### Added

* add all-caps auto-moderation ([`3336a64`])
* reuse one forum thread per user instead of creating a new one per action ([`b31c229`])
* add durable UserWarning table backing /warn strikes ([`9666e94`])
* link member name to Discord profile in join embed ([`f8ccfc0`])
* add member join logging and per-user ignore list; bump version to 1.7.0 ([`a312bdd`])
* implement move messages and thread relocation commands ([`6fda099`])
* add event audit logging and deploy secret wiring ([`0867f28`])
* show offending image in cross-channel spam log embed ([`81f8266`])
* show username and id in list output ([`efa57b5`])
* add persistent background history backfill with resume capability ([`0448781`])
* update changelog for version 1.6.8; fix single-message enable interaction timeout and update version number ([`3d52a09`])
* update changelog for version 1.6.7; add configurable cross-channel spam enforcement options (delete messages and timeout) ([`70c8694`])
* update changelog for version 1.6.6; implement optional content for live test and enhance detection handling ([`2006eca`])
* update changelog for version 1.6.5; add attachment support to spam live test ([`c37f826`])
* update changelog for version 1.6.4; enhance cross-channel spam detection and add live test command ([`1c6d25a`])
* add command access controls and moderation exemptions to deployment configuration; update README ([`5f70da2`])
* add moderation exemptions and command access controls; update configuration and services ([`73317d0`])
* implement cross-channel spam detection module and enhance single-message enforcement ([`c12cbad`])
* add moderation log and cross-channel spam configuration to .env file ([`b06a27d`])
* add ModerationLogService, CrossChannelSpamDetector, and moderation action logging ([`e1b422f`])
* add single-message channel enforcement feature and update documentation ([`ddf64fe`])
* wire SingleMessage commands, service and tests for PandaBot ([`c9ff323`])
* add SingleMessageService for PandaBot ([`0328bc7`])
* add SingleMessage EF migration for PandaBot ([`73fd4e7`])
* add SingleMessage model classes for PandaBot ([`04b79b9`])
* enhance deployment process with staging directory for service and journald config files ([`f959e06`])
* add Prometheus metrics endpoint and update version to 1.5.37 ([`c2b5907`])
* add GUILD_ID environment variable to .env file and update documentation ([`00fd422`])
* update version to 1.5.36 and enhance HTTP request handling in HeartbeatMonitorService ([`c10e26d`])
* update version to 1.5.35 and downgrade disconnect lifecycle warnings to informational logging ([`59ac90e`])
* update version to 1.5.34 and enhance slash command registration handling ([`87107d5`])
* update version to 1.5.33 and implement single registration for slash commands ([`5e38f4a`])
* add deployment workflow for Digital Ocean and update README references ([`fb82e9a`])
* add Discord lifecycle observability with connection and disconnection logging ([`c48ade2`])
* add validation for BotConfig and HeartbeatConfig; update version to 1.5.6 ([`c391494`])
* enhance Discord configuration binding with error handling and diagnostics ([`4b78760`])
* wire heartbeat deploy secrets and enhance service health checks; bump version to 1.5.4 ([`a779b06`])
* implement configurable uptime heartbeat monitor and related services ([`8458bcd`])
* add VersionManager tests and update changelog handling ([`ab0cf73`])
* add new custom words for improved spell checking ([`61c24ea`])
* enhance sunrise and sunset display with real-world time conversion ([`b50c287`])
* add sunrise and sunset calculations to location time info ([`58df44b`])
* add in-game date properties and enhance date calculation in VerseTimeService ([`0d15621`])
* enhance location selection interaction and improve fuzzy matching logic ([`3241a7d`])
* update version to 1.4.2, enhance location selection with dropdown, and improve fuzzy matching logic ([`066bece`])
* update AI instructions and changelog for clarity and consistency ([`ca39f08`])
* update version to 1.4.1 and enhance fuzzy location search algorithm ([`1998740`])
* add /sc time command for local time retrieval and implement VerseTime service ([`2ca0e28`])
* implement automatic cache versioning for vehicle cache ([`e173e23`])
* use full terminal name in location display ([`070e9d6`])
* enhance commodity location display with full system hierarchy ([`bd61db9`])
* display full location names instead of terminal codes ([`2968d16`])
* display top 5 prices for buy/sell/purchase/rental ([`b3527ba`])
* auto-populate UEX vehicle cache on startup ([`fe68f2f`])
* Add vehicle purchase and rental price models with fetching methods ([`b58abe9`])
* Enhance vehicle pricing model with additional sale properties and currency information ([`ef17357`])
* Enhance vehicle option descriptions and derive vehicle types from API response ([`8d2c717`])
* Add UEX vehicle caching functionality ([`9c559bd`])
* Enhance UEX item caching by adding category fetching and updating response handling ([`3136ef8`])
* Add UEXItemCacheInitializerService to initialize item cache on startup ([`827bd99`])
* Update UEXCommodityService to use new API endpoints and improve data handling ([`149ef97`])
* Integrate UEX API configuration and update services for commodity pricing ([`121b3e6`])
* Add UEX commodity service and command for fetching commodity prices ([`4da607e`])
* Implement git-aware version tracking and add check-commits command for version validation ([`5c17bd7`])
* Implement automatic build validation for version consistency and update version to 1.2.3 ([`469deb3`])
* Add Discord.Commands namespace and update group attributes for Path of Exile, Return of Reckoning, and Star Citizen modules ([`3e98f0d`])
* Update to use ROR API for player count and status retrieval docs: Enhance GitHub Copilot instructions with PowerShell command guidelines docs: Update changelog for version 1.2.2 with ROR API integration details ([`1fbee15`])
* Update bot description and add game modules configuration section ([`05268e5`])
* Enhance `/about` command with version, module, and command counts ([`80e6df3`])
* Enhance logging during startup and service registration ([`c53127a`])
* Enhance module loading with timeout and detailed logging ([`efcc2e4`])
* Implement timeout for module loading and enhance logging during startup ([`7a25112`])
* Add GameModulesConfig for enabling/disabling game modules and update service registration ([`a44b9f8`])
* Add services and commands for Return of Reckoning server status ([`13bcf50`])
* Add new words to cspell configuration for improved spell checking ([`57163cb`])
* Add methods for profession level conversion in AshesRecipeService ([`89fb401`])
* Add initial migration and create tables for CachedItems, CachedMobs, CachedVendors, GuildSettings, CachedCraftingRecipes, MobItemDrops, CachedRecipeIngredients, and MobRecipeDrops ([`c263e0e`])
* Implement VersionManager for synchronizing .csproj and CHANGELOG versions ([`ad73cf8`])
* Add Path of Exile status command group ([`b9edc72`])
* Add .NET VersionManager tool for version synchronization ([`afc90a9`])
* Add automated version and changelog management ([`c1c383d`])
* Add Star Citizen status command using RSI status API ([`2ef49c5`])
* Add 'serverinfo' to custom words in cspell configuration ([`5ba93fd`])
* Regenerate database snapshot and update version to 1.0.1 ([`932df68`])
* Add bot version display in /serverinfo command and log version on startup ([`273f705`])
* Add CertificationLevel column to CachedCraftingRecipes migration ([`8a9a8d8`])
* Refactor deployment workflow and enhance JSON property extraction utilities ([`626f55b`])
* Add deployment setup guide and service configuration for PandaBot ([`ee7f439`])
* Enhance profession level handling and add recursive profession requirement fetching for crafting recipes ([`bf98032`])
* Enhance crafting and recipe services to display required skill levels and improve profession handling ([`9ae581d`])
* Enhance item and recipe services to calculate and display highest required skill levels for crafting ([`182225a`])
* Refactor profession level handling to improve readability and support certification levels from JSON ([`dcc86db`])
* Enhance level parsing logic to support tier and Roman numeral suffixes for crafting recipes ([`815d13d`])
* Enhance recipe processing logic to skip purification recipes and handle circular references in ingredients ([`8014f71`])
* Update recipe fetching logic to return raw JSON and enhance error handling ([`c350b6d`])
* Add API integration for fetching item recipes and enhance logging for recipe retrieval ([`7db454c`])
* Improve caching and enrichment logic in AshesForgeApiService and AshesRecipeService ([`b633f93`])
* Enhance recipe command with improved logging and background enrichment for recipes ([`2b48484`])
* Enhance AshesForgeApiService and AshesRecipeService with improved logging and caching for item and recipe details ([`6720c10`])
* Add Ashes of Creation module with item and recipe commands ([`30e77ab`])
* Implement AshesForge data caching services and item management ([`3675ffc`])

### Fixed

* fix mod-log thread lookup limit, /warnings ordering, and missing permission ([`0037eb3`])
* pin SQLitePCLRaw.bundle_e_sqlite3 to 3.0.3 to resolve CVE-2025-6965 ([`9e5a466`])
* enable service on boot via systemctl enable --now ([`176688d`])
* show username and profile image in member join/leave embeds ([`c671e99`])
* download image before deletion, delete follow-on messages from confirmed spammers ([`d65805f`])
* suppress delete log for bot and activity users in all resolution paths ([`b1ad84d`])
* store message snapshots for ignored users to enable correct delete suppression ([`08ad033`])
* restore deleted author mention and avatar icon ([`459c60e`])
* guarantee deleted-message avatar fallback ([`0733646`])
* stabilize deleted author name and avatar attribution ([`62c0178`])
* add receive-time fallback for deleted author attribution ([`5f0aca6`])
* enable message cache for deleted author capture ([`9e68008`])
* improve deleted message author attribution from audit logs ([`acec6e4`])
* add singlemessage backfill migration ([`70e31e4`])
* resolve startup hang in DiscordBotService by ensuring _readyCompletionSource is set in finally block ([`d487daa`])
* update deployment process to use staging directory for service files and journald configuration ([`35ade35`])
* handle null exception in DisconnectedAsync logging ([`27a2920`])
* enhance SSH key handling in deployment workflow ([`a32e017`])
* enhance suppression of gateway log noise during reconnects ([`000214e`])
* suppress empty messages from Discord gateway during reconnects ([`2dbf3ac`])
* suppress empty gateway reconnect noise ([`5967587`])
* reduce false downtime during redeploy ([`f557a78`])
* enhance category processing with detailed logging and error tracking ([`69ee6d5`])
* treat data-null category payloads as empty ([`98242fc`])
* reduce noise for empty category item responses ([`13f5dd0`])
* query UEX items by category to satisfy API requirements ([`70abbd4`])
* add bearer token auth and configurable base URL to UEXVehicleService ([`f6b42e6`])
* add User-Agent header to all Star Citizen API HTTP clients ([`e456b7e`])
* add UEX cache auto-refresh and item cache initialization ([`425a580`])
* correct formatting for location search fuzzy matching entry ([`c9d05c0`])
* clarify commodity price labels as buy/sell prices ([`00dbcb1`])
* filter zero prices in commodity embed display ([`79f4808`])
* extract manufacturer from company_name field in UEX API ([`294947d`])
* Refactor location formatting logic for improved clarity and accuracy ([`365370c`])
* Update dropdown component handling to delete original response message after selection ([`bce5fd7`])
* Add logging for first price data and enrich location information in vehicle pricing ([`fef00b9`])
* Remove dropdown components from original response in vehicle and item commands ([`99bc12d`])
* Enhance vehicle data handling and improve price display in UEX vehicle service ([`64db563`])
* Update item pricing retrieval to use item ID instead of name for improved accuracy ([`43dbbc1`])
* Update item selection handling to ensure user-specific interactions and improve error feedback ([`1e572c4`])
* Update FollowupAsync calls to set ephemeral parameter for improved user feedback ([`67c1796`])
* Update caching logic to use expiration cutoff for item retrieval ([`249d7b1`])
* Correct repository root path and enhance cross-platform compatibility for version validation ([`a0ca99f`])
* Enhance error handling and logging during module loading ([`1c84902`])
* Update constructor to accept HttpClient directly instead of IHttpClientFactory ([`2ede280`])
* Refactor GetProfessionLevel method to use JsonHelper for property extraction ([`fa142fb`])
* Allow VersionManager source in git while ignoring build artifacts ([`a4d9d9d`])
* Update Path of Exile service to use correct API endpoints ([`6c2cb74`])
* Correct pre-commit hook regex for Windows and update setup docs ([`183b7b2`])
* Use RSI status index.json endpoint instead of blocked API endpoint ([`04a4ed3`])
* Add User-Agent header to Star Citizen status API requests ([`0775de3`])
* Update version to 1.0.3 in project file ([`88e3073`])
* Update version to 1.0.2 and adjust migration model snapshot for table definitions ([`f75d186`])
* Update deployment script to move .env file with sudo and improve security ([`e70606f`])
* Update deployment workflow to include code checkout and service file deployment ([`527f04a`])
* Update user in pandabot.service from 'deployment' to 'pandabot' ([`d06469f`])
* Remove defaultValueSql from CertificationLevel column in CachedCraftingRecipes migration ([`8100ba4`])
* Update default value for CertificationLevel column in CachedCraftingRecipes migration ([`0fefe92`])
* Update workflow triggers and enhance README with build status badge ([`c776e74`])
* Add database migration logic to the application startup process ([`21ffaab`])
* Improve deployment process by refining rsync commands for file transfer and cleanup ([`4de402d`])
* Optimize deployment process by simplifying rsync commands for file transfer ([`bf3224e`])
* Enhance deployment process with rsync for efficient file transfer and cleanup ([`9f1a6c6`])
* Correct deployment user in workflow and enhance .env file creation ([`355a4b8`])
* Update project name and deployment paths in workflow and solution files ([`bc30cfe`])
* Resolve merge conflict in Directory.Packages.props and update package versions ([`3095d5f`])

### Changed

* simplify workflow by removing deployment steps and renaming to .NET CI ([`4dab53c`])
* Replace ModifyOriginalResponseAsync with FollowupAsync for improved response handling ([`051b3f9`])
* Clean up project file by removing unused package references and folders ([`c18e05c`])

## [1.8.0] - 2026-07-04

### Added

* add all-caps auto-moderation ([`3336a64`])
* reuse one forum thread per user instead of creating a new one per action ([`b31c229`])
* add durable UserWarning table backing /warn strikes ([`9666e94`])
* link member name to Discord profile in join embed ([`f8ccfc0`])
* add member join logging and per-user ignore list; bump version to 1.7.0 ([`a312bdd`])
* implement move messages and thread relocation commands ([`6fda099`])
* add event audit logging and deploy secret wiring ([`0867f28`])
* show offending image in cross-channel spam log embed ([`81f8266`])
* show username and id in list output ([`efa57b5`])
* add persistent background history backfill with resume capability ([`0448781`])
* update changelog for version 1.6.8; fix single-message enable interaction timeout and update version number ([`3d52a09`])
* update changelog for version 1.6.7; add configurable cross-channel spam enforcement options (delete messages and timeout) ([`70c8694`])
* update changelog for version 1.6.6; implement optional content for live test and enhance detection handling ([`2006eca`])
* update changelog for version 1.6.5; add attachment support to spam live test ([`c37f826`])
* update changelog for version 1.6.4; enhance cross-channel spam detection and add live test command ([`1c6d25a`])
* add command access controls and moderation exemptions to deployment configuration; update README ([`5f70da2`])
* add moderation exemptions and command access controls; update configuration and services ([`73317d0`])
* implement cross-channel spam detection module and enhance single-message enforcement ([`c12cbad`])
* add moderation log and cross-channel spam configuration to .env file ([`b06a27d`])
* add ModerationLogService, CrossChannelSpamDetector, and moderation action logging ([`e1b422f`])
* add single-message channel enforcement feature and update documentation ([`ddf64fe`])
* wire SingleMessage commands, service and tests for PandaBot ([`c9ff323`])
* add SingleMessageService for PandaBot ([`0328bc7`])
* add SingleMessage EF migration for PandaBot ([`73fd4e7`])
* add SingleMessage model classes for PandaBot ([`04b79b9`])
* enhance deployment process with staging directory for service and journald config files ([`f959e06`])
* add Prometheus metrics endpoint and update version to 1.5.37 ([`c2b5907`])
* add GUILD_ID environment variable to .env file and update documentation ([`00fd422`])
* update version to 1.5.36 and enhance HTTP request handling in HeartbeatMonitorService ([`c10e26d`])
* update version to 1.5.35 and downgrade disconnect lifecycle warnings to informational logging ([`59ac90e`])
* update version to 1.5.34 and enhance slash command registration handling ([`87107d5`])
* update version to 1.5.33 and implement single registration for slash commands ([`5e38f4a`])
* add deployment workflow for Digital Ocean and update README references ([`fb82e9a`])
* add Discord lifecycle observability with connection and disconnection logging ([`c48ade2`])
* add validation for BotConfig and HeartbeatConfig; update version to 1.5.6 ([`c391494`])
* enhance Discord configuration binding with error handling and diagnostics ([`4b78760`])
* wire heartbeat deploy secrets and enhance service health checks; bump version to 1.5.4 ([`a779b06`])
* implement configurable uptime heartbeat monitor and related services ([`8458bcd`])
* add VersionManager tests and update changelog handling ([`ab0cf73`])
* add new custom words for improved spell checking ([`61c24ea`])
* enhance sunrise and sunset display with real-world time conversion ([`b50c287`])
* add sunrise and sunset calculations to location time info ([`58df44b`])
* add in-game date properties and enhance date calculation in VerseTimeService ([`0d15621`])
* enhance location selection interaction and improve fuzzy matching logic ([`3241a7d`])
* update version to 1.4.2, enhance location selection with dropdown, and improve fuzzy matching logic ([`066bece`])
* update AI instructions and changelog for clarity and consistency ([`ca39f08`])
* update version to 1.4.1 and enhance fuzzy location search algorithm ([`1998740`])
* add /sc time command for local time retrieval and implement VerseTime service ([`2ca0e28`])
* implement automatic cache versioning for vehicle cache ([`e173e23`])
* use full terminal name in location display ([`070e9d6`])
* enhance commodity location display with full system hierarchy ([`bd61db9`])
* display full location names instead of terminal codes ([`2968d16`])
* display top 5 prices for buy/sell/purchase/rental ([`b3527ba`])
* auto-populate UEX vehicle cache on startup ([`fe68f2f`])
* Add vehicle purchase and rental price models with fetching methods ([`b58abe9`])
* Enhance vehicle pricing model with additional sale properties and currency information ([`ef17357`])
* Enhance vehicle option descriptions and derive vehicle types from API response ([`8d2c717`])
* Add UEX vehicle caching functionality ([`9c559bd`])
* Enhance UEX item caching by adding category fetching and updating response handling ([`3136ef8`])
* Add UEXItemCacheInitializerService to initialize item cache on startup ([`827bd99`])
* Update UEXCommodityService to use new API endpoints and improve data handling ([`149ef97`])
* Integrate UEX API configuration and update services for commodity pricing ([`121b3e6`])
* Add UEX commodity service and command for fetching commodity prices ([`4da607e`])
* Implement git-aware version tracking and add check-commits command for version validation ([`5c17bd7`])
* Implement automatic build validation for version consistency and update version to 1.2.3 ([`469deb3`])
* Add Discord.Commands namespace and update group attributes for Path of Exile, Return of Reckoning, and Star Citizen modules ([`3e98f0d`])
* Update to use ROR API for player count and status retrieval docs: Enhance GitHub Copilot instructions with PowerShell command guidelines docs: Update changelog for version 1.2.2 with ROR API integration details ([`1fbee15`])
* Update bot description and add game modules configuration section ([`05268e5`])
* Enhance `/about` command with version, module, and command counts ([`80e6df3`])
* Enhance logging during startup and service registration ([`c53127a`])
* Enhance module loading with timeout and detailed logging ([`efcc2e4`])
* Implement timeout for module loading and enhance logging during startup ([`7a25112`])
* Add GameModulesConfig for enabling/disabling game modules and update service registration ([`a44b9f8`])
* Add services and commands for Return of Reckoning server status ([`13bcf50`])
* Add new words to cspell configuration for improved spell checking ([`57163cb`])
* Add methods for profession level conversion in AshesRecipeService ([`89fb401`])
* Add initial migration and create tables for CachedItems, CachedMobs, CachedVendors, GuildSettings, CachedCraftingRecipes, MobItemDrops, CachedRecipeIngredients, and MobRecipeDrops ([`c263e0e`])
* Implement VersionManager for synchronizing .csproj and CHANGELOG versions ([`ad73cf8`])
* Add Path of Exile status command group ([`b9edc72`])
* Add .NET VersionManager tool for version synchronization ([`afc90a9`])
* Add automated version and changelog management ([`c1c383d`])
* Add Star Citizen status command using RSI status API ([`2ef49c5`])
* Add 'serverinfo' to custom words in cspell configuration ([`5ba93fd`])
* Regenerate database snapshot and update version to 1.0.1 ([`932df68`])
* Add bot version display in /serverinfo command and log version on startup ([`273f705`])
* Add CertificationLevel column to CachedCraftingRecipes migration ([`8a9a8d8`])
* Refactor deployment workflow and enhance JSON property extraction utilities ([`626f55b`])
* Add deployment setup guide and service configuration for PandaBot ([`ee7f439`])
* Enhance profession level handling and add recursive profession requirement fetching for crafting recipes ([`bf98032`])
* Enhance crafting and recipe services to display required skill levels and improve profession handling ([`9ae581d`])
* Enhance item and recipe services to calculate and display highest required skill levels for crafting ([`182225a`])
* Refactor profession level handling to improve readability and support certification levels from JSON ([`dcc86db`])
* Enhance level parsing logic to support tier and Roman numeral suffixes for crafting recipes ([`815d13d`])
* Enhance recipe processing logic to skip purification recipes and handle circular references in ingredients ([`8014f71`])
* Update recipe fetching logic to return raw JSON and enhance error handling ([`c350b6d`])
* Add API integration for fetching item recipes and enhance logging for recipe retrieval ([`7db454c`])
* Improve caching and enrichment logic in AshesForgeApiService and AshesRecipeService ([`b633f93`])
* Enhance recipe command with improved logging and background enrichment for recipes ([`2b48484`])
* Enhance AshesForgeApiService and AshesRecipeService with improved logging and caching for item and recipe details ([`6720c10`])
* Add Ashes of Creation module with item and recipe commands ([`30e77ab`])
* Implement AshesForge data caching services and item management ([`3675ffc`])

### Fixed

* enable service on boot via systemctl enable --now ([`176688d`])
* show username and profile image in member join/leave embeds ([`c671e99`])
* download image before deletion, delete follow-on messages from confirmed spammers ([`d65805f`])
* suppress delete log for bot and activity users in all resolution paths ([`b1ad84d`])
* store message snapshots for ignored users to enable correct delete suppression ([`08ad033`])
* restore deleted author mention and avatar icon ([`459c60e`])
* guarantee deleted-message avatar fallback ([`0733646`])
* stabilize deleted author name and avatar attribution ([`62c0178`])
* add receive-time fallback for deleted author attribution ([`5f0aca6`])
* enable message cache for deleted author capture ([`9e68008`])
* improve deleted message author attribution from audit logs ([`acec6e4`])
* add singlemessage backfill migration ([`70e31e4`])
* resolve startup hang in DiscordBotService by ensuring _readyCompletionSource is set in finally block ([`d487daa`])
* update deployment process to use staging directory for service files and journald configuration ([`35ade35`])
* handle null exception in DisconnectedAsync logging ([`27a2920`])
* enhance SSH key handling in deployment workflow ([`a32e017`])
* enhance suppression of gateway log noise during reconnects ([`000214e`])
* suppress empty messages from Discord gateway during reconnects ([`2dbf3ac`])
* suppress empty gateway reconnect noise ([`5967587`])
* reduce false downtime during redeploy ([`f557a78`])
* enhance category processing with detailed logging and error tracking ([`69ee6d5`])
* treat data-null category payloads as empty ([`98242fc`])
* reduce noise for empty category item responses ([`13f5dd0`])
* query UEX items by category to satisfy API requirements ([`70abbd4`])
* add bearer token auth and configurable base URL to UEXVehicleService ([`f6b42e6`])
* add User-Agent header to all Star Citizen API HTTP clients ([`e456b7e`])
* add UEX cache auto-refresh and item cache initialization ([`425a580`])
* correct formatting for location search fuzzy matching entry ([`c9d05c0`])
* clarify commodity price labels as buy/sell prices ([`00dbcb1`])
* filter zero prices in commodity embed display ([`79f4808`])
* extract manufacturer from company_name field in UEX API ([`294947d`])
* Refactor location formatting logic for improved clarity and accuracy ([`365370c`])
* Update dropdown component handling to delete original response message after selection ([`bce5fd7`])
* Add logging for first price data and enrich location information in vehicle pricing ([`fef00b9`])
* Remove dropdown components from original response in vehicle and item commands ([`99bc12d`])
* Enhance vehicle data handling and improve price display in UEX vehicle service ([`64db563`])
* Update item pricing retrieval to use item ID instead of name for improved accuracy ([`43dbbc1`])
* Update item selection handling to ensure user-specific interactions and improve error feedback ([`1e572c4`])
* Update FollowupAsync calls to set ephemeral parameter for improved user feedback ([`67c1796`])
* Update caching logic to use expiration cutoff for item retrieval ([`249d7b1`])
* Correct repository root path and enhance cross-platform compatibility for version validation ([`a0ca99f`])
* Enhance error handling and logging during module loading ([`1c84902`])
* Update constructor to accept HttpClient directly instead of IHttpClientFactory ([`2ede280`])
* Refactor GetProfessionLevel method to use JsonHelper for property extraction ([`fa142fb`])
* Allow VersionManager source in git while ignoring build artifacts ([`a4d9d9d`])
* Update Path of Exile service to use correct API endpoints ([`6c2cb74`])
* Correct pre-commit hook regex for Windows and update setup docs ([`183b7b2`])
* Use RSI status index.json endpoint instead of blocked API endpoint ([`04a4ed3`])
* Add User-Agent header to Star Citizen status API requests ([`0775de3`])
* Update version to 1.0.3 in project file ([`88e3073`])
* Update version to 1.0.2 and adjust migration model snapshot for table definitions ([`f75d186`])
* Update deployment script to move .env file with sudo and improve security ([`e70606f`])
* Update deployment workflow to include code checkout and service file deployment ([`527f04a`])
* Update user in pandabot.service from 'deployment' to 'pandabot' ([`d06469f`])
* Remove defaultValueSql from CertificationLevel column in CachedCraftingRecipes migration ([`8100ba4`])
* Update default value for CertificationLevel column in CachedCraftingRecipes migration ([`0fefe92`])
* Update workflow triggers and enhance README with build status badge ([`c776e74`])
* Add database migration logic to the application startup process ([`21ffaab`])
* Improve deployment process by refining rsync commands for file transfer and cleanup ([`4de402d`])
* Optimize deployment process by simplifying rsync commands for file transfer ([`bf3224e`])
* Enhance deployment process with rsync for efficient file transfer and cleanup ([`9f1a6c6`])
* Correct deployment user in workflow and enhance .env file creation ([`355a4b8`])
* Update project name and deployment paths in workflow and solution files ([`bc30cfe`])
* Resolve merge conflict in Directory.Packages.props and update package versions ([`3095d5f`])

### Changed

* simplify workflow by removing deployment steps and renaming to .NET CI ([`4dab53c`])
* Replace ModifyOriginalResponseAsync with FollowupAsync for improved response handling ([`051b3f9`])
* Clean up project file by removing unused package references and folders ([`c18e05c`])

## [1.7.2] - 2026-06-30

### Fixed

* Link member name to Discord profile in join audit embed

## [1.7.1] - 2026-06-26

### Fixed

* Show username and profile image in member join and leave audit embeds

## [1.7.0] - 2026-06-25

### Added

* Add member join logging and per-user ignore list for event audit

## [1.6.20] - 2026-06-25

### Changed

* Add move messages and thread relocation with reaction and pinned metadata copy

## [1.6.19] - 2026-06-24

### Changed

* Restore clickable deleted-message author mention and author icon rendering

## [1.6.18] - 2026-06-24

### Changed

* Guarantee deleted-message avatar thumbnail fallback

## [1.6.17] - 2026-06-24

### Changed

* Stabilize deleted message attribution with early capture and author profile fallback

## [1.6.16] - 2026-06-24

### Changed

* Add receive-time snapshot fallback for deleted message author attribution

## [1.6.15] - 2026-06-24

### Changed

* Enable message cache for deleted-message author attribution

## [1.6.14] - 2026-06-24

### Changed

* Improve deleted message author attribution from audit logs

## [1.6.13] - 2026-06-24

### Changed

* Add moderation event audit logging and deploy secret wiring

## [1.6.12] - 2026-06-20

### Added

* show username and id in list output ([`efa57b5`])
* add persistent background history backfill with resume capability ([`0448781`])
* update changelog for version 1.6.8; fix single-message enable interaction timeout and update version number ([`3d52a09`])
* update changelog for version 1.6.7; add configurable cross-channel spam enforcement options (delete messages and timeout) ([`70c8694`])
* update changelog for version 1.6.6; implement optional content for live test and enhance detection handling ([`2006eca`])
* update changelog for version 1.6.5; add attachment support to spam live test ([`c37f826`])
* update changelog for version 1.6.4; enhance cross-channel spam detection and add live test command ([`1c6d25a`])
* add command access controls and moderation exemptions to deployment configuration; update README ([`5f70da2`])
* add moderation exemptions and command access controls; update configuration and services ([`73317d0`])
* implement cross-channel spam detection module and enhance single-message enforcement ([`c12cbad`])
* add moderation log and cross-channel spam configuration to .env file ([`b06a27d`])
* add ModerationLogService, CrossChannelSpamDetector, and moderation action logging ([`e1b422f`])
* add single-message channel enforcement feature and update documentation ([`ddf64fe`])
* wire SingleMessage commands, service and tests for PandaBot ([`c9ff323`])
* add SingleMessageService for PandaBot ([`0328bc7`])
* add SingleMessage EF migration for PandaBot ([`73fd4e7`])
* add SingleMessage model classes for PandaBot ([`04b79b9`])
* enhance deployment process with staging directory for service and journald config files ([`f959e06`])
* add Prometheus metrics endpoint and update version to 1.5.37 ([`c2b5907`])
* add GUILD_ID environment variable to .env file and update documentation ([`00fd422`])
* update version to 1.5.36 and enhance HTTP request handling in HeartbeatMonitorService ([`c10e26d`])
* update version to 1.5.35 and downgrade disconnect lifecycle warnings to informational logging ([`59ac90e`])
* update version to 1.5.34 and enhance slash command registration handling ([`87107d5`])
* update version to 1.5.33 and implement single registration for slash commands ([`5e38f4a`])
* add deployment workflow for Digital Ocean and update README references ([`fb82e9a`])
* add Discord lifecycle observability with connection and disconnection logging ([`c48ade2`])
* add validation for BotConfig and HeartbeatConfig; update version to 1.5.6 ([`c391494`])
* enhance Discord configuration binding with error handling and diagnostics ([`4b78760`])
* wire heartbeat deploy secrets and enhance service health checks; bump version to 1.5.4 ([`a779b06`])
* implement configurable uptime heartbeat monitor and related services ([`8458bcd`])
* add VersionManager tests and update changelog handling ([`ab0cf73`])
* add new custom words for improved spell checking ([`61c24ea`])
* enhance sunrise and sunset display with real-world time conversion ([`b50c287`])
* add sunrise and sunset calculations to location time info ([`58df44b`])
* add in-game date properties and enhance date calculation in VerseTimeService ([`0d15621`])
* enhance location selection interaction and improve fuzzy matching logic ([`3241a7d`])
* update version to 1.4.2, enhance location selection with dropdown, and improve fuzzy matching logic ([`066bece`])
* update AI instructions and changelog for clarity and consistency ([`ca39f08`])
* update version to 1.4.1 and enhance fuzzy location search algorithm ([`1998740`])
* add /sc time command for local time retrieval and implement VerseTime service ([`2ca0e28`])
* implement automatic cache versioning for vehicle cache ([`e173e23`])
* use full terminal name in location display ([`070e9d6`])
* enhance commodity location display with full system hierarchy ([`bd61db9`])
* display full location names instead of terminal codes ([`2968d16`])
* display top 5 prices for buy/sell/purchase/rental ([`b3527ba`])
* auto-populate UEX vehicle cache on startup ([`fe68f2f`])
* Add vehicle purchase and rental price models with fetching methods ([`b58abe9`])
* Enhance vehicle pricing model with additional sale properties and currency information ([`ef17357`])
* Enhance vehicle option descriptions and derive vehicle types from API response ([`8d2c717`])
* Add UEX vehicle caching functionality ([`9c559bd`])
* Enhance UEX item caching by adding category fetching and updating response handling ([`3136ef8`])
* Add UEXItemCacheInitializerService to initialize item cache on startup ([`827bd99`])
* Update UEXCommodityService to use new API endpoints and improve data handling ([`149ef97`])
* Integrate UEX API configuration and update services for commodity pricing ([`121b3e6`])
* Add UEX commodity service and command for fetching commodity prices ([`4da607e`])
* Implement git-aware version tracking and add check-commits command for version validation ([`5c17bd7`])
* Implement automatic build validation for version consistency and update version to 1.2.3 ([`469deb3`])
* Add Discord.Commands namespace and update group attributes for Path of Exile, Return of Reckoning, and Star Citizen modules ([`3e98f0d`])
* Update to use ROR API for player count and status retrieval docs: Enhance GitHub Copilot instructions with PowerShell command guidelines docs: Update changelog for version 1.2.2 with ROR API integration details ([`1fbee15`])
* Update bot description and add game modules configuration section ([`05268e5`])
* Enhance `/about` command with version, module, and command counts ([`80e6df3`])
* Enhance logging during startup and service registration ([`c53127a`])
* Enhance module loading with timeout and detailed logging ([`efcc2e4`])
* Implement timeout for module loading and enhance logging during startup ([`7a25112`])
* Add GameModulesConfig for enabling/disabling game modules and update service registration ([`a44b9f8`])
* Add services and commands for Return of Reckoning server status ([`13bcf50`])
* Add new words to cspell configuration for improved spell checking ([`57163cb`])
* Add methods for profession level conversion in AshesRecipeService ([`89fb401`])
* Add initial migration and create tables for CachedItems, CachedMobs, CachedVendors, GuildSettings, CachedCraftingRecipes, MobItemDrops, CachedRecipeIngredients, and MobRecipeDrops ([`c263e0e`])
* Implement VersionManager for synchronizing .csproj and CHANGELOG versions ([`ad73cf8`])
* Add Path of Exile status command group ([`b9edc72`])
* Add .NET VersionManager tool for version synchronization ([`afc90a9`])
* Add automated version and changelog management ([`c1c383d`])
* Add Star Citizen status command using RSI status API ([`2ef49c5`])
* Add 'serverinfo' to custom words in cspell configuration ([`5ba93fd`])
* Regenerate database snapshot and update version to 1.0.1 ([`932df68`])
* Add bot version display in /serverinfo command and log version on startup ([`273f705`])
* Add CertificationLevel column to CachedCraftingRecipes migration ([`8a9a8d8`])
* Refactor deployment workflow and enhance JSON property extraction utilities ([`626f55b`])
* Add deployment setup guide and service configuration for PandaBot ([`ee7f439`])
* Enhance profession level handling and add recursive profession requirement fetching for crafting recipes ([`bf98032`])
* Enhance crafting and recipe services to display required skill levels and improve profession handling ([`9ae581d`])
* Enhance item and recipe services to calculate and display highest required skill levels for crafting ([`182225a`])
* Refactor profession level handling to improve readability and support certification levels from JSON ([`dcc86db`])
* Enhance level parsing logic to support tier and Roman numeral suffixes for crafting recipes ([`815d13d`])
* Enhance recipe processing logic to skip purification recipes and handle circular references in ingredients ([`8014f71`])
* Update recipe fetching logic to return raw JSON and enhance error handling ([`c350b6d`])
* Add API integration for fetching item recipes and enhance logging for recipe retrieval ([`7db454c`])
* Improve caching and enrichment logic in AshesForgeApiService and AshesRecipeService ([`b633f93`])
* Enhance recipe command with improved logging and background enrichment for recipes ([`2b48484`])
* Enhance AshesForgeApiService and AshesRecipeService with improved logging and caching for item and recipe details ([`6720c10`])
* Add Ashes of Creation module with item and recipe commands ([`30e77ab`])
* Implement AshesForge data caching services and item management ([`3675ffc`])

### Fixed

* add singlemessage backfill migration ([`70e31e4`])
* resolve startup hang in DiscordBotService by ensuring _readyCompletionSource is set in finally block ([`d487daa`])
* update deployment process to use staging directory for service files and journald configuration ([`35ade35`])
* handle null exception in DisconnectedAsync logging ([`27a2920`])
* enhance SSH key handling in deployment workflow ([`a32e017`])
* enhance suppression of gateway log noise during reconnects ([`000214e`])
* suppress empty messages from Discord gateway during reconnects ([`2dbf3ac`])
* suppress empty gateway reconnect noise ([`5967587`])
* reduce false downtime during redeploy ([`f557a78`])
* enhance category processing with detailed logging and error tracking ([`69ee6d5`])
* treat data-null category payloads as empty ([`98242fc`])
* reduce noise for empty category item responses ([`13f5dd0`])
* query UEX items by category to satisfy API requirements ([`70abbd4`])
* add bearer token auth and configurable base URL to UEXVehicleService ([`f6b42e6`])
* add User-Agent header to all Star Citizen API HTTP clients ([`e456b7e`])
* add UEX cache auto-refresh and item cache initialization ([`425a580`])
* correct formatting for location search fuzzy matching entry ([`c9d05c0`])
* clarify commodity price labels as buy/sell prices ([`00dbcb1`])
* filter zero prices in commodity embed display ([`79f4808`])
* extract manufacturer from company_name field in UEX API ([`294947d`])
* Refactor location formatting logic for improved clarity and accuracy ([`365370c`])
* Update dropdown component handling to delete original response message after selection ([`bce5fd7`])
* Add logging for first price data and enrich location information in vehicle pricing ([`fef00b9`])
* Remove dropdown components from original response in vehicle and item commands ([`99bc12d`])
* Enhance vehicle data handling and improve price display in UEX vehicle service ([`64db563`])
* Update item pricing retrieval to use item ID instead of name for improved accuracy ([`43dbbc1`])
* Update item selection handling to ensure user-specific interactions and improve error feedback ([`1e572c4`])
* Update FollowupAsync calls to set ephemeral parameter for improved user feedback ([`67c1796`])
* Update caching logic to use expiration cutoff for item retrieval ([`249d7b1`])
* Correct repository root path and enhance cross-platform compatibility for version validation ([`a0ca99f`])
* Enhance error handling and logging during module loading ([`1c84902`])
* Update constructor to accept HttpClient directly instead of IHttpClientFactory ([`2ede280`])
* Refactor GetProfessionLevel method to use JsonHelper for property extraction ([`fa142fb`])
* Allow VersionManager source in git while ignoring build artifacts ([`a4d9d9d`])
* Update Path of Exile service to use correct API endpoints ([`6c2cb74`])
* Correct pre-commit hook regex for Windows and update setup docs ([`183b7b2`])
* Use RSI status index.json endpoint instead of blocked API endpoint ([`04a4ed3`])
* Add User-Agent header to Star Citizen status API requests ([`0775de3`])
* Update version to 1.0.3 in project file ([`88e3073`])
* Update version to 1.0.2 and adjust migration model snapshot for table definitions ([`f75d186`])
* Update deployment script to move .env file with sudo and improve security ([`e70606f`])
* Update deployment workflow to include code checkout and service file deployment ([`527f04a`])
* Update user in pandabot.service from 'deployment' to 'pandabot' ([`d06469f`])
* Remove defaultValueSql from CertificationLevel column in CachedCraftingRecipes migration ([`8100ba4`])
* Update default value for CertificationLevel column in CachedCraftingRecipes migration ([`0fefe92`])
* Update workflow triggers and enhance README with build status badge ([`c776e74`])
* Add database migration logic to the application startup process ([`21ffaab`])
* Improve deployment process by refining rsync commands for file transfer and cleanup ([`4de402d`])
* Optimize deployment process by simplifying rsync commands for file transfer ([`bf3224e`])
* Enhance deployment process with rsync for efficient file transfer and cleanup ([`9f1a6c6`])
* Correct deployment user in workflow and enhance .env file creation ([`355a4b8`])
* Update project name and deployment paths in workflow and solution files ([`bc30cfe`])
* Resolve merge conflict in Directory.Packages.props and update package versions ([`3095d5f`])

### Changed

* simplify workflow by removing deployment steps and renaming to .NET CI ([`4dab53c`])
* Replace ModifyOriginalResponseAsync with FollowupAsync for improved response handling ([`051b3f9`])
* Clean up project file by removing unused package references and folders ([`c18e05c`])

## [1.6.11] - 2026-06-18

### Changed

* Show username and user ID in /singlemessage list output

## [1.6.10] - 2026-06-18

### Changed

* Add EF migration for persistent single-message backfill startup fix

## [1.6.9] - 2026-06-18

### Changed

* Add persistent background single-message history backfill

## [1.6.8] - 2026-06-18

### Changed

* Fix single-message enable interaction timeout by deferring response

## [1.6.7] - 2026-06-15

### Changed

* Add configurable cross-channel spam enforcement defaults (delete+timeout on)

## [1.6.6] - 2026-06-15

### Changed

* Fix live test detection race via TCS; make content optional (attachment-only test now supported)

## [1.6.5] - 2026-06-15

### Changed

* Fix cross-channel live test detection state and add attachment-aware spam test support

## [1.6.4] - 2026-06-15

### Changed

* Improve cross-channel spam detection fingerprinting, logging, and add cleanup-enabled live testing

## [1.6.3] - 2026-06-15

### Changed

* Add moderation exemptions, command access controls, and forum log resolution fallback

## [1.6.2] - 2026-06-15

### Fixed

* `DiscordBotService` no longer hangs for 60 seconds on startup when a transient disconnect occurs during the post-Ready delay — `_readyCompletionSource` is now set in a `finally` block so it fires on all exit paths

## [1.6.1] - 2026-06-15

### Changed

* `SingleMessageService` is now fully DB-backed — channel registration no longer requires an `appsettings.json`/env-var config entry; `/singlemessage enable` and `/singlemessage disable` operate directly on the database at runtime with no redeploy needed
* `/singlemessage enable` gains a `scan_history` parameter (default `true`) replacing the old per-channel config flag
* `/singlemessage list` now shows enforcement status (active / disabled) alongside posted users

### Added

* `/spam test` command (requires Manage Messages) — dry-runs the cross-channel spam detector against any text, showing the computed fingerprint, current config, trigger conditions, and enforcement actions without taking any real action

## [1.6.0] - 2026-06-14

### Changed

* Add single-message-per-user channel enforcement with /singlemessage slash commands

## [1.5.37] - 2026-05-27

### Changed

* Add Prometheus metrics endpoint

## [1.5.36] - 2026-05-19

### Changed

* Version bump

## [1.5.35] - 2026-05-19

### Changed

* Downgrade disconnect lifecycle warnings to informational logging

## [1.5.34] - 2026-05-19

### Changed

* Harmonize reconnect-safe slash command registration and transient disconnect logging

## [1.5.33] - 2026-05-19

### Changed

* Register slash commands only once; skip re-registration on gateway reconnects

## [1.5.32] - 2026-05-19

### Changed

* Downgrade graceful Discord disconnect log from Warning to Information

## [1.5.31] - 2026-05-14

### Changed

* Suppress EF Core SQL command logs at Serilog level

## [1.5.30] - 2026-05-14

### Changed

* Add size and retention limits to PandaBot file log rotation

## [1.5.29] - 2026-05-14

### Changed

* Suppress noisy HttpClient info logs

## [1.5.28] - 2026-05-14

### Changed

* Reduce UEX category fetch warning noise for transient server errors

## [1.5.27] - 2026-05-14

### Changed

* Reduce EF Core SQL command log verbosity for UEX operations

## [1.5.26] - 2026-05-14

### Changed

* Restore recursive deploy preflight ownership to prevent rsync permission errors

## [1.5.25] - 2026-05-14

### Changed

* Fix deploy chmod step to run under sudo

## [1.5.24] - 2026-05-14

### Changed

* Make deploy preflight sudo-compatible and disable rsync timestamp preservation

## [1.5.23] - 2026-05-14

### Changed

* Validate decoded deploy SSH secret is a private key and normalize CRLF

## [1.5.22] - 2026-05-14

### Changed

* Fix preflight chown to recursively change ownership of all deploy subdirectories

## [1.5.21] - 2026-05-14

### Changed

* Skip rsync timestamp preservation to avoid permission errors on host-owned files

## [1.5.20] - 2026-05-14

### Changed

* Harden deploy SSH key parsing for CI secrets

## [1.5.19] - 2026-05-14

### Changed

* Fix rsync group metadata deploy failures

## [1.5.18] - 2026-05-14

### Changed

* Harden deploy preflight permissions for deploy path

## [1.5.17] - 2026-05-14

### Changed

* Harden Dependabot updates for NuGet and GitHub Actions

## [1.5.16] - 2026-05-14

### Changed

* Align CI and deploy workflows with Halo pattern

## [1.5.15] - 2026-05-14

### Changed

* Add Discord lifecycle observability and readiness hardening

## [1.5.14] - 2026-05-14

### Changed

* Suppress gateway placeholder log noise more robustly

## [1.5.13] - 2026-05-13

### Changed

* Suppress empty Discord gateway noise events

## [1.5.12] - 2026-05-13

### Changed

* Send startup and shutdown heartbeat pings for smoother redeploys

## [1.5.11] - 2026-05-13

### Changed

* Treat UEX data-null category payloads as empty results

## [1.5.10] - 2026-05-13

### Changed

* Reduce noisy UEX category format warning handling

## [1.5.9] - 2026-05-12

### Changed

* fix UEX item cache refresh to query items by required category filter

## [1.5.8] - 2026-05-12

### Changed

* fix UEXVehicleService to support bearer token authentication and configurable base URL

## [1.5.7] - 2026-05-12

### Changed

* fix Star Citizen API requests by adding User-Agent header to all HTTP clients

## [1.5.6] - 2026-05-12

### Changed

* add config validation for input parameters and secrets

## [1.5.5] - 2026-05-12

### Changed

* add config binding diagnostics and stronger deploy service checks

## [1.5.4] - 2026-05-12

### Changed

* wire heartbeat deploy secrets and harden service health checks

## [1.5.3] - 2026-05-12

### Changed

* add configurable uptime heartbeat monitor

## \[1.5.2] - 2026-04-27

### PATCH

* Upgrade to .NET 10 (SDK 10.0.203), target framework net10.0, and pin CI runners to ubuntu-24.04

## \[1.5.1] - 2026-02-21

### PATCH

* Fix UEX cache auto-refresh and add item cache initialization
  All notable changes to PandaBot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## \[1.5.0] - 2026-02-12

### Added

* feat: add sunrise/sunset times and dual time display for locations

## \[1.4.2] - 2026-02-11

### Fixed

* Fix location search fuzzy matching and add interactive dropdown for disambiguation

## \[1.4.1] - 2026-02-11

### Fixed

* Improve fuzzy location search to handle spaces and typos (Area 18 vs Area18)

## \[1.4.0] - 2026-02-11

### Added

* Add /sc time command to show Star Citizen local times using VerseTime data

## \[1.3.1] - 2026-02-11

### Fixed

* fix(star-citizen): add automatic cache versioning system
* Add CacheVersion field to VehicleCache for automatic invalidation
* Fix vehicle type derivation from UEX API boolean flags
* Migration automatically clears outdated cache entries
* Cache rebuilds with proper vehicle types on startup
* No manual database deletion needed on production

## \[1.3.0] - 2026-02-05

### Added

* Implement SC Ship / Vehicle and Item commands

## \[1.2.4] - 2026-02-05

### Changed

* chore: change ashes command to aoc and adopt short command names across modules

## \[1.2.3] - 2026-02-05

### Changed

* Add full command names with aliases and PowerShell documentation updates

## \[1.2.2] - 2026-02-05

### Fixed

* Return of Reckoning player count accuracy
  * Switched from HTML web scraping (unreliable) to official ROR API endpoint
  * Now uses `https://api.returnofreckoning.com/stats/online_list_new.php?realm_id=1`
  * More accurate and faster player count reporting
  * Better error handling for API responses

## \[1.2.1] - 2026-02-05

### Changed

* Enhanced `/about` command
  * Now displays bot version number
  * Shows count of loaded modules and registered commands
  * Displays Discord.Net framework version
  * Better organized fields for quick reference

## \[1.2.0] - 2026-02-05

### Added

* Return of Reckoning (ROR) server status module
  * New `/ror status` slash command to check ROR server status and player counts
  * Web scraper service that fetches real-time data from returnofreckoning.com
  * Color-coded embed responses (🟢 Online / 🔴 Offline) with player information
* Configurable game modules system
  * New `GameModules` configuration section in appsettings.json
  * Feature flags to enable/disable each game module independently
  * Conditional dependency injection - only loads services when module is enabled
  * Supports: Ashes of Creation, Star Citizen, Path of Exile, Return of Reckoning

### Changed

* Enhanced startup diagnostics and logging
  * Comprehensive logging at every startup phase (host creation, DI registration, DB migrations, module loading, gateway connection)
  * Module discovery now displays all found modules before loading
  * Login and client startup phases separately logged for better troubleshooting
  * Added timeout protection (30s for module loading, 60s for ready signal)

### Fixed

* Critical DI issue preventing bot startup
  * Fixed RORModule to use runtime service resolution instead of constructor injection
  * Allows modules to load even when their dependencies aren't registered
  * Matches pattern used by existing StarCitizen and PathOfExile modules
* Ashes of Creation service issues
  * Resolved merge conflicts in AshesForgeApiService and AshesRecipeService
  * Fixed missing `GetProfessionLevelFromName` and `GetLevelNameFromNumber` helper methods
  * Fixed JsonHelper method calls with proper prefix in GetProfessionLevel()
* Disabled Ashes of Creation by default in production
  * Set `EnableAshesOfCreation: false` in appsettings.json
  * Reduces unnecessary API calls and memory usage when module not actively used
  * Prevents memory cache allocation when module disabled

## \[1.1.1] - 2026-02-01

### Fixed

* Fix Path of Exile API endpoint parsing

## \[1.1.0] - 2026-02-01

### Added

* Add Path of Exile status command

## \[1.0.4] - 2026-02-01

### Fixed

* Star Citizen status API endpoint
  * Changed from blocked `/api/v2/components.json` to publicly accessible `/index.json`
  * Simplified status display showing overall status and per-system status

## \[1.0.3] - 2026-02-01

### Added

* Star Citizen server status command (`/starcitizen status`)
  * Fetches real-time status from RSI status API
  * Groups components by category (Game Servers, Website, etc.)
  * Color-coded status indicators with emoji (✅ Operational, ⚠️ Degraded, 🔴 Partial Outage, ❌ Major Outage)

### Changed

* Database migration system cleaned and consolidated
  * Removed all incremental migrations (7 migration files)
  * Created single `InitialCreate` migration from current model
  * Ensures clean database schema with all properties in sync

### Fixed

* Entity Framework Core model/snapshot mismatch resolved
  * Removed snapshot file and let EF Core regenerate
  * Consolidated migrations to prevent future sync issues
  * Service now runs as correct user (`pandabot` instead of `deployment`)

## \[1.0.2] - 2026-01-31

### Added

* Service file deployment in GitHub Actions
* Passwordless sudo configuration for deployment commands
* Improved .env file handling in deployment

## \[1.0.1] - 2026-01-31

### Added

* Version bump system
* Version displayed in bot startup logs
* Bot version shown in `/serverinfo` command

## \[1.0.0] - 2026-01-31

### Added

* Initial release
* Discord bot with slash commands
* Ashes of Creation integration (items, recipes, vendors, mobs)
* Caching system for API data
* Image caching for fast response times
* Versioning system with startup logging

### Fixed

* Entity Framework Core migration warnings converted to errors
* Model snapshot properly reflects all properties

## Version Bumping Guidelines

**IMPORTANT: Every code change must increment the version in [`PandaBot.csproj`](src/PandaBot/PandaBot.csproj)**

### Semantic Versioning (MAJOR.MINOR.PATCH)

* **PATCH** (1.0.X): Bug fixes, minor improvements, dependency updates
* **MINOR** (1.X.0): New features, new commands, significant improvements
* **MAJOR** (X.0.0): Breaking changes, major architectural changes

### Steps for Version Bumping

1. Make your code changes
2. Update `<Version>X.Y.Z</Version>` in [`PandaBot.csproj`](src/PandaBot/PandaBot.csproj)
3. Add an entry to this CHANGELOG.md under the new version
4. Commit with message: `chore: bump version to X.Y.Z`
5. The GitHub Actions workflow will build and deploy with the new version

### Example

```xml
<!-- Before -->
<Version>1.0.0</Version>

<!-- After (for bug fix) -->
<Version>1.0.1</Version>
```

**The version in the `.csproj` file is the source of truth. Always keep it in sync with the CHANGELOG.**




