import { useEffect, useState, type FormEvent } from 'react';
import './App.css';
import type { SystemStatus, Repository, FolderInspectionResult } from './types/system';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5149';

export default function App() {
  // System status state
  const [status, setStatus] = useState<SystemStatus | null>(null);
  const [statusLoading, setStatusLoading] = useState<boolean>(true);
  const [statusError, setStatusError] = useState<string | null>(null);

  // Repositories state
  const [repositories, setRepositories] = useState<Repository[]>([]);
  const [reposLoading, setReposLoading] = useState<boolean>(true);
  const [reposError, setReposError] = useState<string | null>(null);

  // Repository form state
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [sourceType, setSourceType] = useState('Local');
  const [repositoryUrl, setRepositoryUrl] = useState('');
  const [formSubmitting, setFormSubmitting] = useState(false);
  const [formSuccessMessage, setFormSuccessMessage] = useState<string | null>(null);
  const [formFieldErrors, setFormFieldErrors] = useState<Record<string, string[]>>({});
  const [formGeneralError, setFormGeneralError] = useState<string | null>(null);

  // Folder inspection state
  const [inspectPath, setInspectPath] = useState('.');
  const [inspectLoading, setInspectLoading] = useState(false);
  const [inspectError, setInspectError] = useState<string | null>(null);
  const [inspectionResult, setInspectionResult] = useState<FolderInspectionResult | null>(null);

  const fetchStatus = async () => {
    setStatusLoading(true);
    setStatusError(null);
    try {
      const response = await fetch(`${API_BASE_URL}/api/v1/system/status`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status} (${response.statusText})`);
      }
      const data: SystemStatus = await response.json();
      setStatus(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown connection error';
      setStatusError(`Unable to connect to backend at ${API_BASE_URL}: ${message}`);
      setStatus(null);
    } finally {
      setStatusLoading(false);
    }
  };

  const fetchRepositories = async () => {
    setReposLoading(true);
    setReposError(null);
    try {
      const response = await fetch(`${API_BASE_URL}/api/v1/repositories`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status} (${response.statusText})`);
      }
      const data: Repository[] = await response.json();
      setRepositories(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch repositories';
      setReposError(message);
    } finally {
      setReposLoading(false);
    }
  };

  useEffect(() => {
    fetchStatus();
    fetchRepositories();
  }, []);

  const handleCreateRepository = async (e: FormEvent) => {
    e.preventDefault();
    setFormSubmitting(true);
    setFormFieldErrors({});
    setFormGeneralError(null);
    setFormSuccessMessage(null);

    const payload = {
      name: name.trim(),
      description: description.trim() || null,
      sourceType,
      repositoryUrl: repositoryUrl.trim() || null
    };

    try {
      const response = await fetch(`${API_BASE_URL}/api/v1/repositories`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      if (response.status === 201) {
        const created: Repository = await response.json();
        setFormSuccessMessage(`Repository '${created.name}' created successfully.`);
        setName('');
        setDescription('');
        setRepositoryUrl('');
        setSourceType('Local');
        await fetchRepositories();
      } else if (response.status === 400 || response.status === 409) {
        const errorData = await response.json();
        if (errorData.errors) {
          setFormFieldErrors(errorData.errors);
        } else {
          setFormGeneralError(errorData.detail || errorData.title || 'Validation error occurred.');
        }
      } else {
        setFormGeneralError(`Unexpected server response: HTTP ${response.status}`);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Network error';
      setFormGeneralError(`Failed to submit: ${message}`);
    } finally {
      setFormSubmitting(false);
    }
  };

  const handleInspectFolder = async (e: FormEvent) => {
    e.preventDefault();
    setInspectLoading(true);
    setInspectError(null);
    setInspectionResult(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/v1/code-analysis/inspect`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ directoryPath: inspectPath.trim() || '.' })
      });

      if (response.ok) {
        const data: FolderInspectionResult = await response.json();
        setInspectionResult(data);
      } else {
        const errorData = await response.json().catch(() => ({}));
        setInspectError(errorData.detail || errorData.title || `Inspection failed (HTTP ${response.status})`);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Network error';
      setInspectError(`Failed to inspect folder: ${message}`);
    } finally {
      setInspectLoading(false);
    }
  };

  return (
    <div className="container">
      <header className="header">
        <h1 className="title">CodeAtlas</h1>
        <p className="subtitle">AI-assisted Git repository hosting &amp; code understanding platform</p>
      </header>

      <main>
        {/* System Health Section */}
        <section className="card">
          <div className="card-header-row">
            <h2 className="card-title">Backend Connectivity</h2>
            <button className="refresh-button" onClick={fetchStatus} disabled={statusLoading}>
              {statusLoading ? 'Checking...' : 'Refresh Status'}
            </button>
          </div>

          {statusLoading && (
            <div className="status-badge status-loading">
              <span>Checking backend connection...</span>
            </div>
          )}

          {!statusLoading && statusError && (
            <div>
              <div className="status-badge status-error">
                <span>Disconnected</span>
              </div>
              <p className="error-message">{statusError}</p>
            </div>
          )}

          {!statusLoading && status && (
            <div>
              <div className="status-badge status-healthy">
                <span>● Connected ({status.status})</span>
              </div>

              <div className="details-grid">
                <div className="detail-item">
                  <span className="detail-label">Service</span>
                  <span className="detail-value">{status.service}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Version</span>
                  <span className="detail-value">{status.version}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Environment</span>
                  <span className="detail-value">{status.environment}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Last Checked</span>
                  <span className="detail-value">{new Date(status.timestampUtc).toLocaleTimeString()}</span>
                </div>
              </div>

              <div style={{ marginTop: '1.25rem' }}>
                <span className="detail-label">Active Monolith Modules ({status.activeModules.length})</span>
                <div className="modules-list">
                  {status.activeModules.map((moduleName) => (
                    <span key={moduleName} className="module-pill">
                      {moduleName}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          )}
        </section>

        {/* Local Folder Inspection Section */}
        <section className="card">
          <div className="card-header-row">
            <h2 className="card-title">Read-Only Local Folder Inspection</h2>
          </div>
          <p className="info-note" style={{ marginBottom: '1rem' }}>
            Safely enumerates files and extensions within the configured workspace root boundary without executing or modifying code.
          </p>

          <form onSubmit={handleInspectFolder}>
            <div className="form-group">
              <label className="form-label" htmlFor="inspect-path">
                Directory Path (relative to workspace root)
              </label>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <input
                  id="inspect-path"
                  type="text"
                  className="form-input"
                  style={{ flex: 1 }}
                  placeholder=". or subfolder path (e.g., src)"
                  value={inspectPath}
                  onChange={(e) => setInspectPath(e.target.value)}
                />
                <button type="submit" className="primary-button" disabled={inspectLoading}>
                  {inspectLoading ? 'Inspecting...' : 'Inspect Folder'}
                </button>
              </div>
            </div>
          </form>

          {inspectLoading && (
            <div className="status-badge status-loading" style={{ marginTop: '0.75rem' }}>
              <span>Enumerating files in {inspectPath}...</span>
            </div>
          )}

          {inspectError && <p className="error-message">{inspectError}</p>}

          {inspectionResult && (
            <div style={{ marginTop: '1.5rem' }}>
              <div className="details-grid">
                <div className="detail-item">
                  <span className="detail-label">Inspected Path</span>
                  <span className="detail-value">{inspectionResult.inspectedPath}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Total Files</span>
                  <span className="detail-value">{inspectionResult.totalFiles}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Unique Extensions</span>
                  <span className="detail-value">
                    {Object.keys(inspectionResult.extensionCounts).length}
                  </span>
                </div>
              </div>

              {/* Observed Classifications */}
              <div style={{ marginTop: '1.25rem' }}>
                <span className="detail-label">Observed Project Classifications</span>
                <div className="modules-list">
                  {inspectionResult.projectClassifications.length > 0 ? (
                    inspectionResult.projectClassifications.map((c) => (
                      <span key={c} className="class-badge">
                        {c}
                      </span>
                    ))
                  ) : (
                    <span style={{ color: '#6b7280', fontSize: '0.875rem' }}>
                      None observed (empty or unclassified file types)
                    </span>
                  )}
                </div>
              </div>

              {/* Extension Breakdown */}
              {Object.keys(inspectionResult.extensionCounts).length > 0 && (
                <div style={{ marginTop: '1.25rem' }}>
                  <span className="detail-label">File Counts by Extension</span>
                  <div className="modules-list" style={{ marginTop: '0.5rem' }}>
                    {Object.entries(inspectionResult.extensionCounts)
                      .sort((a, b) => b[1] - a[1])
                      .map(([ext, count]) => (
                        <span key={ext} className="module-pill">
                          {ext}: <strong>{count}</strong>
                        </span>
                      ))}
                  </div>
                </div>
              )}

              {/* Relative File Paths */}
              <div style={{ marginTop: '1.25rem' }}>
                <span className="detail-label">
                  Discovered Files ({inspectionResult.relativeFilePaths.length})
                </span>
                <div className="file-list-box" style={{ marginTop: '0.5rem' }}>
                  {inspectionResult.relativeFilePaths.length > 0 ? (
                    inspectionResult.relativeFilePaths.map((path) => (
                      <div key={path} className="file-list-item">
                        {path}
                      </div>
                    ))
                  ) : (
                    <div style={{ padding: '0.5rem', color: '#6b7280' }}>
                      No files found in directory.
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}
        </section>

        {/* Repository Registration Form */}
        <section className="card">
          <h2 className="card-title">Register Repository</h2>
          <p className="info-note" style={{ marginBottom: '1rem' }}>
            Metadata registration without database persistence. In-memory data resets when API restarts.
          </p>

          <form onSubmit={handleCreateRepository}>
            <div className="form-grid">
              <div className="form-group">
                <label className="form-label" htmlFor="repo-name">
                  Repository Name *
                </label>
                <input
                  id="repo-name"
                  type="text"
                  className="form-input"
                  placeholder="e.g. backend-api"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  maxLength={100}
                  required
                />
                {formFieldErrors['Name'] && (
                  <p className="field-error">{formFieldErrors['Name'].join(' ')}</p>
                )}
              </div>

              <div className="form-group">
                <label className="form-label" htmlFor="repo-source-type">
                  Source Type *
                </label>
                <select
                  id="repo-source-type"
                  className="form-select"
                  value={sourceType}
                  onChange={(e) => setSourceType(e.target.value)}
                >
                  <option value="Local">Local</option>
                  <option value="Remote">Remote</option>
                </select>
                {formFieldErrors['SourceType'] && (
                  <p className="field-error">{formFieldErrors['SourceType'].join(' ')}</p>
                )}
              </div>

              <div className="form-group full-width">
                <label className="form-label" htmlFor="repo-url">
                  Repository URL (optional)
                </label>
                <input
                  id="repo-url"
                  type="url"
                  className="form-input"
                  placeholder="https://github.com/org/repo.git"
                  value={repositoryUrl}
                  onChange={(e) => setRepositoryUrl(e.target.value)}
                  maxLength={2000}
                />
                {formFieldErrors['RepositoryUrl'] && (
                  <p className="field-error">{formFieldErrors['RepositoryUrl'].join(' ')}</p>
                )}
              </div>

              <div className="form-group full-width">
                <label className="form-label" htmlFor="repo-desc">
                  Description (optional)
                </label>
                <textarea
                  id="repo-desc"
                  className="form-textarea"
                  placeholder="Short description of the repository and code scope"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  maxLength={500}
                />
                {formFieldErrors['Description'] && (
                  <p className="field-error">{formFieldErrors['Description'].join(' ')}</p>
                )}
              </div>
            </div>

            {formGeneralError && <p className="error-message">{formGeneralError}</p>}
            {formSuccessMessage && <div className="success-message">{formSuccessMessage}</div>}

            <div style={{ marginTop: '1rem' }}>
              <button type="submit" className="primary-button" disabled={formSubmitting}>
                {formSubmitting ? 'Registering...' : 'Register Repository'}
              </button>
            </div>
          </form>
        </section>

        {/* Repositories List Section */}
        <section className="card">
          <div className="card-header-row">
            <h2 className="card-title">Registered Repositories ({repositories.length})</h2>
            <button
              className="refresh-button"
              onClick={fetchRepositories}
              disabled={reposLoading}
            >
              {reposLoading ? 'Refreshing...' : 'Refresh List'}
            </button>
          </div>

          {reposLoading && repositories.length === 0 && (
            <div className="status-badge status-loading">
              <span>Loading repositories...</span>
            </div>
          )}

          {reposError && <p className="error-message">Error: {reposError}</p>}

          {!reposLoading && repositories.length === 0 && !reposError && (
            <div className="empty-state">
              <p>No repositories registered yet.</p>
              <p style={{ marginTop: '0.25rem', fontSize: '0.8125rem' }}>
                Use the form above to add your first repository.
              </p>
            </div>
          )}

          {repositories.length > 0 && (
            <table className="repo-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Source</th>
                  <th>Registered</th>
                  <th>URL</th>
                </tr>
              </thead>
              <tbody>
                {repositories.map((repo) => (
                  <tr key={repo.id}>
                    <td>
                      <div className="repo-name">{repo.name}</div>
                      {repo.description && <div className="repo-desc">{repo.description}</div>}
                    </td>
                    <td>
                      <span
                        className={`source-badge ${
                          repo.sourceType.toLowerCase() === 'local' ? 'source-local' : 'source-remote'
                        }`}
                      >
                        {repo.sourceType}
                      </span>
                    </td>
                    <td style={{ color: '#6b7280', fontSize: '0.8125rem' }}>
                      {new Date(repo.createdAtUtc).toLocaleDateString()}
                    </td>
                    <td style={{ fontSize: '0.8125rem' }}>
                      {repo.repositoryUrl ? (
                        <a
                          href={repo.repositoryUrl}
                          target="_blank"
                          rel="noreferrer noopener"
                          style={{ color: '#2563eb', textDecoration: 'none' }}
                        >
                          View Source ↗
                        </a>
                      ) : (
                        <span style={{ color: '#9ca3af' }}>—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>

        {/* Platform Architecture Note */}
        <section className="card">
          <h2 className="card-title">Platform Architecture</h2>
          <p style={{ color: '#6b7280', fontSize: '0.9375rem' }}>
            CodeAtlas is structured as a modular monolith in ASP.NET Core (.NET 10) with an extractable background worker service. Repository operations are encapsulated within the <code>CodeAtlas.Modules.Repositories</code> boundary, and folder inspection runs strictly within the <code>CodeAtlas.Modules.CodeAnalysis</code> module.
          </p>
        </section>
      </main>
    </div>
  );
}
