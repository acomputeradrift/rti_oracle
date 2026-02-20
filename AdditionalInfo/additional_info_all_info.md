# Additional Info - Merged Notes

## Scope
- This document merges existing AdditionalInfo section Markdown docs.
- The scope is limited to AdditionalInfo upload, extraction, and lookup preparation.
- The scope includes uploading the Additional Info spreadsheet.
  Value: Confirmed
  Evidence: project_data_plan.md :: Scope
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: UploadAdditionalInfo_Click
- The scope includes validating sheet names.
  Value: Confirmed
  Evidence: project_data_plan.md :: Scope
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
- The scope includes validating headers.
  Value: Confirmed
  Evidence: project_data_plan.md :: Scope
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: ApplySchema
- The scope includes building lookup data for downstream mapping.
  Value: Confirmed
  Evidence: project_data_plan.md :: Scope
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: ApplySchema
- The guide does not prescribe concrete code signatures.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Scope
- The plan documents intended behavior only.
  Value: Confirmed
  Evidence: project_data_plan.md :: Scope
- DriverProfile parsing logic remains in the DriverProfile module.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Scope Confirmation
- The processing flow should avoid changes to unrelated features.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow

## Goal
- Load Additional Info from a `.xlsx` file.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Goal
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: UploadAdditionalInfo_Click
- Prepare lookup data for downstream driverProfile mapping.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Goal
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: ApplySchema
- Attach parsed AdditionalInfo outputs to `ProjectDataBundle.Additional`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: InitializeProcessing

## Existing Entry Points And Patterns
- Upload entry point: `MainWindow.xaml.cs` (`UploadAdditionalInfo_Click`).
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Existing Entry Points and Patterns
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: UploadAdditionalInfo_Click
- UI controls: `UI/Panels/ProjectDataPanel.xaml`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Existing Entry Points and Patterns
  Value: Implemented
  Evidence: OracleByFPCLtd/UI/Panels/ProjectDataPanel.xaml.cs :: ProjectDataPanel
- Settings history components: `Settings/Services/AdditionalInfoService.cs` and `Settings/Models/OracleSettings.cs`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Existing Entry Points and Patterns
  Value: Implemented
  Evidence: OracleByFPCLtd/Settings/Services/AdditionalInfoService.cs :: RecordAdditionalInfo
- Project data extraction flow: `ProjectData/ProjectDataExtraction.cs`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Existing Entry Points and Patterns
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/ProjectDataExtraction.cs :: ProjectDataExtractor.Extract
- AdditionalInfo extractor hook: `ProjectData/Extractors/AdditionalDataExtractor.cs`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Existing Entry Points and Patterns
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: LoadAdditionalData
- Data container: `ProjectData/Models/ProjectDataBundle.cs` (`AdditionalData`).
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Existing Entry Points and Patterns
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Models/ProjectDataBundle.cs :: ProjectDataBundle
- Driver profiles live under `DriverProfiles/*`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Existing Entry Points and Patterns
  Value: Implemented
  Evidence: OracleByFPCLtd/DriverProfiles/Catalog/DriverProfileCatalog.cs :: DriverProfileCatalog.All

## Inputs
- Additional Info spreadsheet (`.xlsx`).
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Required Inputs
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: UploadAdditionalInfo_Click
- `.apex` extraction output that includes driver device names.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Required Inputs
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: LoadAdditionalData
- DriverProfile schemas used to parse matched sheets.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Required Inputs
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
- Raw log files may be used only to confirm sheet needs and indices.
  Value: Confirmed
  Evidence: project_data_plan.md :: Inputs

## Sheet Matching Rules
- Sheet tab names must match driver device names from `.apex` data.
  Value: Confirmed
  Evidence: project_data_plan.md :: Spreadsheet Sheets and Schemas
- Matching uses stable driver device names, not display names.
  Value: Confirmed
  Evidence: project_data_plan.md :: Spreadsheet Sheets and Schemas
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: LoadAdditionalData
- Matching is exact (`sheet name == driver device name`).
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
- Non-matching sheets must not be loaded.
  Value: Confirmed
  Evidence: project_data_plan.md :: Spreadsheet Sheets and Schemas
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
- Non-matching sheets must produce recorded errors.
  Value: Confirmed
  Evidence: project_data_plan.md :: Spreadsheet Sheets and Schemas
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
- Loading must continue for matching sheets.
  Value: Confirmed
  Evidence: project_data_plan.md :: Spreadsheet Sheets and Schemas
  Value: Implemented
  Evidence: OracleByFPCLtd.Tests/AdditionalDataExtractorTests.cs :: ExtractReportsUnmatchedSheetsAndMissingProfiles

## Parsing Rules
- Each matching sheet must resolve to its DriverProfile.
  Value: Confirmed
  Evidence: project_data_plan.md :: Upload and Ingestion Plan
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
- Each matching sheet must be parsed by its DriverProfile schema.
  Value: Confirmed
  Evidence: project_data_plan.md :: Upload and Ingestion Plan
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: ApplySchema
- Required headers must be validated during parsing.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: ApplySchema
- AdditionalInfo parsing must not hardcode per-driver columns.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
- Parsing behavior must defer to DriverProfile schema definitions.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: ApplySchema

## Data Output Contract
- Build in-memory lookup maps from DriverProfile schema outputs.
  Value: Confirmed
  Evidence: project_data_plan.md :: Upload and Ingestion Plan
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: AddMapping
- Store parsed outputs keyed by driver device name.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: ApplySchema
- Keep Additional data structures generic and schema-aligned.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
- Attach lookup maps to `ProjectDataBundle.Additional` for processing use.
  Value: Confirmed
  Evidence: project_data_plan.md :: Upload and Ingestion Plan
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: InitializeProcessing
- Provide read-only lookup maps to the DriverProfile module.
  Value: Confirmed
  Evidence: project_data_plan.md :: Output Contract to Driver Profile Module
  Value: Implemented
  Evidence: OracleByFPCLtd/DriverProfiles/Models/DriverProfileModels.cs :: IDriverProfileMapper.TryMap

## Processing Integration
- Capture selected AdditionalInfo file path in memory in `MainWindow.xaml.cs`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: UploadAdditionalInfo_Click
- Keep existing recent-file behavior through `AdditionalInfoService` and settings.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: UploadAdditionalInfo_Click
- Build driver device name sets from `.apex` extraction output in `MainWindow.xaml.cs`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: LoadAdditionalData
- Run `AdditionalDataExtractor` after `.apex` extraction completes.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: LoadProjectDataForProcessingAsync
- Attach extractor results to `ProjectDataBundle`.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/MainWindow.xaml.cs :: InitializeProcessing

## Error Handling
- Record errors for non-matching sheet tabs.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
- Record errors for DriverProfile schema validation failures.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: ApplySchema
- Surface summary errors through existing UI patterns.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Implementation Flow
- Allow successful sheet loads to complete even when some sheets fail.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Goal
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract

## Constraints
- No new dependencies.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Behavior Constraints
- `.apex` and spreadsheet files are read-only inputs.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Behavior Constraints
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/ProjectDataExtraction.cs :: ProjectDataExtractor.Extract
- No inferred mappings outside DriverProfile schema definitions.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Behavior Constraints
- No modifications to unrelated modules.
  Value: Confirmed
  Evidence: additional_info_implimentation_guide.md :: Behavior Constraints

## Open Items
- Confirm DriverProfile schema format for AdditionalInfo extraction.
  Value: Confirmed
  Evidence: project_data_plan.md :: Open Items
- Confirm whether missing entries should return explicit `UNRESOLVED` markers.
  Value: Confirmed
  Evidence: project_data_plan.md :: Open Items

## Source Assertions
- Existing docs state no code changes were made at the time of writing.
  Value: Confirmed
  Evidence: project_data_plan.md :: Scope Confirmation
- Existing docs state no source documents were modified at the time of writing.
  Value: Confirmed
  Evidence: project_data_plan.md :: Scope Confirmation
