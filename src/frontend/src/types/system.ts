export interface SystemStatus {
  status: string;
  service: string;
  environment: string;
  version: string;
  timestampUtc: string;
  activeModules: string[];
}

export interface Repository {
  id: string;
  name: string;
  description: string | null;
  createdAtUtc: string;
  sourceType: string;
  repositoryUrl: string | null;
}

export interface CreateRepositoryRequest {
  name: string;
  description: string;
  sourceType: string;
  repositoryUrl: string;
}

export interface FolderInspectionResult {
  inspectedPath: string;
  totalFiles: number;
  extensionCounts: Record<string, number>;
  relativeFilePaths: string[];
  projectClassifications: string[];
}
