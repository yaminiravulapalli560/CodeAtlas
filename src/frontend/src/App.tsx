
import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import './App.css';
import type {
  SystemStatus,
  Repository,
  FolderInspectionResult,
} from './types/system';

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:5149';

type Theme = 'light' | 'dark';
type Section = 'Overview' | 'Repositories' | 'Code Analysis' | 'Architecture';

const NAV_ITEMS: { label: Section; icon: string }[] = [
  { label: 'Overview', icon: '◫' },
  { label: 'Repositories', icon: '⌘' },
  { label: 'Code Analysis', icon: '</>' },
  { label: 'Architecture', icon: '◇' },
];

function readSavedTheme(): Theme {
  try {
    return localStorage.getItem('codeatlas-theme') === 'dark'
      ? 'dark'
      : 'light';
  } catch {
    return 'light';
  }
}

function SectionHeading({
  eyebrow,
  title,
  description,
  action,
}: {
  eyebrow?: string;
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="section-heading">
      <div>
        {eyebrow && <div className="eyebrow">{eyebrow}</div>}
        <h2 className="section-title">{title}</h2>
        {description && (
          <p className="section-description">{description}</p>
        )}
      </div>
      {action && <div className="section-heading-action">{action}</div>}
    </div>
  );
}

export default function App() {
  const [theme, setTheme] = useState<Theme>(readSavedTheme);
  const [activeSection, setActiveSection] = useState<Section>('Overview');

  // Backend status
  const [status, setStatus] = useState<SystemStatus | null>(null);
  const [statusLoading, setStatusLoading] = useState(true);
  const [statusError, setStatusError] = useState<string | null>(null);

  // Repositories
  const [repositories, setRepositories] = useState<Repository[]>([]);
  const [reposLoading, setReposLoading] = useState(true);
  const [reposError, setReposError] = useState<string | null>(null);

  // Repository registration form
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [sourceType, setSourceType] = useState('Local');
  const [repositoryUrl, setRepositoryUrl] = useState('');
  const [formSubmitting, setFormSubmitting] = useState(false);
  const [formSuccessMessage, setFormSuccessMessage] =
    useState<string | null>(null);
  const [formFieldErrors, setFormFieldErrors] =
    useState<Record<string, string[]>>({});
  const [formGeneralError, setFormGeneralError] =
    useState<string | null>(null);

  // Folder inspection
  const [inspectPath, setInspectPath] = useState('.');
  const [inspectLoading, setInspectLoading] = useState(false);
  const [inspectError, setInspectError] = useState<string | null>(null);
  const [inspectionResult, setInspectionResult] =
    useState<FolderInspectionResult | null>(null);

  // Persist the selected theme
  useEffect(() => {
    document.documentElement.dataset.theme = theme;

    try {
      localStorage.setItem('codeatlas-theme', theme);
    } catch {
      // Theme still works for this session if storage is unavailable.
    }
  }, [theme]);

  const fetchStatus = async () => {
    setStatusLoading(true);
    setStatusError(null);

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/v1/system/status`,
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status} (${response.statusText})`);
      }

      const data: SystemStatus = await response.json();
      setStatus(data);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Unknown connection error';

      setStatusError(
        `Unable to connect to backend at ${API_BASE_URL}: ${message}`,
      );
      setStatus(null);
    } finally {
      setStatusLoading(false);
    }
  };

  const fetchRepositories = async () => {
    setReposLoading(true);
    setReposError(null);

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/v1/repositories`,
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status} (${response.statusText})`);
      }

      const data: Repository[] = await response.json();
      setRepositories(data);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Failed to fetch repositories';

      setReposError(message);
    } finally {
      setReposLoading(false);
    }
  };

  // Load existing data when the dashboard starts
  useEffect(() => {
    void fetchStatus();
    void fetchRepositories();
  }, []);

  const handleCreateRepository = async (
    e: FormEvent<HTMLFormElement>,
  ) => {
    e.preventDefault();

    setFormSubmitting(true);
    setFormFieldErrors({});
    setFormGeneralError(null);
    setFormSuccessMessage(null);

    const payload = {
      name: name.trim(),
      description: description.trim() || null,
      sourceType,
      repositoryUrl: repositoryUrl.trim() || null,
    };

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/v1/repositories`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(payload),
        },
      );

      if (response.status === 201) {
        const created: Repository = await response.json();

        setFormSuccessMessage(
          `Repository '${created.name}' created successfully.`,
        );
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
          setFormGeneralError(
            errorData.detail ||
            errorData.title ||
            'Validation error occurred.',
          );
        }
      } else {
        setFormGeneralError(
          `Unexpected server response: HTTP ${response.status}`,
        );
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Network error';
      setFormGeneralError(`Failed to submit: ${message}`);
    } finally {
      setFormSubmitting(false);
    }
  };

  const handleInspectFolder = async (
    e: FormEvent<HTMLFormElement>,
  ) => {
    e.preventDefault();

    setInspectLoading(true);
    setInspectError(null);
    setInspectionResult(null);

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/v1/code-analysis/inspect`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            directoryPath: inspectPath.trim() || '.',
          }),
        },
      );

      if (response.ok) {
        const data: FolderInspectionResult = await response.json();
        setInspectionResult(data);
      } else {
        const errorData = await response.json().catch(() => ({}));

        setInspectError(
          errorData.detail ||
          errorData.title ||
          `Inspection failed (HTTP ${response.status})`,
        );
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Network error';
      setInspectError(`Failed to inspect folder: ${message}`);
    } finally {
      setInspectLoading(false);
    }
  };

  const toggleTheme = () => {
    setTheme((current) => (current === 'light' ? 'dark' : 'light'));
  };

  const pageDescriptions: Record<Section, string> = {
    Overview: 'A clear view of your repositories and codebase workspace.',
    Repositories:
      'Register and manage the repositories you want to understand.',
    'Code Analysis':
      'Inspect a workspace safely and review its files and project signals.',
    Architecture:
      'Explore the platform structure and planned CodeAtlas capabilities.',
  };

  return (
    <div className={`app-shell theme-${theme}`}>
      {/* Sidebar */}
      <aside className="sidebar">
        <button
          type="button"
          className="brand"
          onClick={() => setActiveSection('Overview')}
          aria-label="Go to CodeAtlas overview"
        >
          <span className="brand-mark">C</span>
          <span className="brand-copy">
            <span className="brand-name">CodeAtlas</span>
            <span className="brand-caption">CODE INTELLIGENCE</span>
          </span>
        </button>

        <div className="nav-label">WORKSPACE</div>

        <nav className="sidebar-nav" aria-label="Main navigation">
          {NAV_ITEMS.map((item) => (
            <button
              key={item.label}
              type="button"
              className={`nav-item ${activeSection === item.label ? 'active' : ''
                }`}
              onClick={() => setActiveSection(item.label)}
              aria-current={
                activeSection === item.label ? 'page' : undefined
              }
            >
              <span className="nav-icon" aria-hidden="true">
                {item.icon}
              </span>
              <span>{item.label}</span>

              {item.label === 'Repositories' && (
                <span className="nav-count">{repositories.length}</span>
              )}
            </button>
          ))}
        </nav>

        <div className="sidebar-bottom">
          <div className="workspace-chip">
            <span
              className={`status-dot ${statusError ? 'offline' : statusLoading ? 'pending' : ''
                }`}
            />
            <span>
              <strong>Local workspace</strong>
              <small>
                {statusLoading
                  ? 'Checking connection…'
                  : statusError
                    ? 'API unavailable'
                    : 'API connection checked'}
              </small>
            </span>
          </div>

          <button
            type="button"
            className="theme-toggle"
            onClick={toggleTheme}
          >
            <span aria-hidden="true">
              {theme === 'light' ? '☾' : '☀'}
            </span>
            {theme === 'light' ? 'Dark appearance' : 'Light appearance'}
          </button>
        </div>
      </aside>

      {/* Main workspace */}
      <div className="workspace">
        <header className="workspace-header">
          <div>
            <div className="breadcrumb">
              Workspace <span>/</span> {activeSection}
            </div>
            <h1 className="workspace-title">{activeSection}</h1>
            <p className="workspace-subtitle">
              {pageDescriptions[activeSection]}
            </p>
          </div>

          <div className="header-actions">
            <span className="environment-pill">
              <span className="environment-dot" />
              Local environment
            </span>

            <button
              type="button"
              className="icon-button"
              onClick={() => {
                void fetchStatus();

                if (
                  activeSection === 'Repositories' ||
                  activeSection === 'Overview'
                ) {
                  void fetchRepositories();
                }
              }}
              aria-label="Refresh workspace data"
              title="Refresh workspace data"
            >
              ↻
            </button>
          </div>
        </header>

        <main className="workspace-content">
          {/* OVERVIEW */}
          {activeSection === 'Overview' && (
            <>
              <section className="welcome-panel">
                <div className="welcome-copy">
                  <div className="eyebrow">
                    YOUR DEVELOPMENT WORKSPACE
                  </div>

                  <h2>
                    Understand your codebase.
                    <br />
                    <span>Move with clarity.</span>
                  </h2>

                  <p>
                    Bring repository details and workspace signals into one
                    focused place. CodeAtlas helps you start with what exists
                    today and grow toward deeper code understanding.
                  </p>

                  <div className="welcome-actions">
                    <button
                      type="button"
                      className="primary-button"
                      onClick={() => setActiveSection('Repositories')}
                    >
                      Explore repositories <span aria-hidden="true">→</span>
                    </button>

                    <button
                      type="button"
                      className="secondary-button"
                      onClick={() => setActiveSection('Code Analysis')}
                    >
                      Inspect a folder
                    </button>
                  </div>
                </div>

                <div className="welcome-art" aria-hidden="true">
                  <div className="orbit orbit-one" />
                  <div className="orbit orbit-two" />
                  <div className="orbit orbit-three" />
                  <div className="atlas-node node-center">CA</div>
                  <div className="atlas-node node-top">&lt;/&gt;</div>
                  <div className="atlas-node node-left">{'{ }'}</div>
                  <div className="atlas-node node-right">◇</div>
                  <div className="atlas-node node-bottom">ƒ</div>
                </div>
              </section>

              {/* Summary cards */}
              <div className="metric-grid">
                <article className="metric-card">
                  <div className="metric-top">
                    <span className="metric-icon violet">⌘</span>
                    <span className="metric-caption">TRACKED</span>
                  </div>
                  <div className="metric-value">{repositories.length}</div>
                  <div className="metric-label">Repositories registered</div>
                </article>

                <article className="metric-card">
                  <div className="metric-top">
                    <span className="metric-icon green">●</span>
                    <span className="metric-caption">SYSTEM</span>
                  </div>
                  <div className="metric-value metric-status">
                    {statusLoading
                      ? 'Checking'
                      : statusError
                        ? 'Offline'
                        : 'Healthy'}
                  </div>
                  <div className="metric-label">Backend connectivity</div>
                </article>

                <article className="metric-card">
                  <div className="metric-top">
                    <span className="metric-icon blue">{'</>'}</span>
                    <span className="metric-caption">WORKSPACE</span>
                  </div>
                  <div className="metric-value">
                    {inspectionResult?.totalFiles ?? '—'}
                  </div>
                  <div className="metric-label">
                    Files in latest inspection
                  </div>
                </article>
              </div>

              <div className="overview-grid">
                {/* Backend status */}
                <section className="card">
                  <SectionHeading
                    eyebrow="SYSTEM"
                    title="Backend connectivity"
                    description="Current API health and registered modules."
                    action={
                      <button
                        type="button"
                        className="refresh-button"
                        onClick={() => void fetchStatus()}
                        disabled={statusLoading}
                      >
                        {statusLoading ? 'Checking…' : 'Refresh'}
                      </button>
                    }
                  />

                  {statusLoading && (
                    <div className="inline-state">
                      <span className="spinner" />
                      Checking backend connection…
                    </div>
                  )}

                  {!statusLoading && statusError && (
                    <div className="notice notice-error">
                      <strong>Backend disconnected</strong>
                      <p>{statusError}</p>
                    </div>
                  )}

                  {!statusLoading && status && (
                    <>
                      <div className="connection-row">
                        <span className="status-indicator" />
                        <div>
                          <strong>{status.service}</strong>
                          <small>
                            {status.status} · Version {status.version}
                          </small>
                        </div>
                        <span className="state-pill success">Connected</span>
                      </div>

                      <div className="details-grid compact-grid">
                        <div className="detail-item">
                          <span className="detail-label">Environment</span>
                          <span className="detail-value">
                            {status.environment}
                          </span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Last checked</span>
                          <span className="detail-value">
                            {new Date(status.timestampUtc).toLocaleTimeString()}
                          </span>
                        </div>
                      </div>

                      <div className="module-area">
                        <span className="detail-label">
                          Active monolith modules · {status.activeModules.length}
                        </span>
                        <div className="modules-list">
                          {status.activeModules.map((moduleName) => (
                            <span key={moduleName} className="module-pill">
                              {moduleName}
                            </span>
                          ))}
                        </div>
                      </div>
                    </>
                  )}
                </section>

                {/* Recent repositories */}
                <section className="card">
                  <SectionHeading
                    eyebrow="RECENT WORK"
                    title="Repositories"
                    description="Your registered repository metadata."
                    action={
                      <button
                        type="button"
                        className="text-button"
                        onClick={() => setActiveSection('Repositories')}
                      >
                        View all →
                      </button>
                    }
                  />

                  {reposLoading && repositories.length === 0 && (
                    <div className="inline-state">
                      <span className="spinner" />
                      Loading repositories…
                    </div>
                  )}

                  {reposError && (
                    <div className="notice notice-error">
                      <p>{reposError}</p>
                      <button
                        type="button"
                        className="text-button"
                        onClick={() => void fetchRepositories()}
                      >
                        Try again
                      </button>
                    </div>
                  )}

                  {!reposLoading &&
                    repositories.length === 0 &&
                    !reposError && (
                      <div className="empty-state compact-empty">
                        <div className="empty-icon">⌘</div>
                        <strong>No repositories yet</strong>
                        <p>
                          Register a repository to start organizing your
                          workspace.
                        </p>
                        <button
                          type="button"
                          className="secondary-button"
                          onClick={() => setActiveSection('Repositories')}
                        >
                          Register repository
                        </button>
                      </div>
                    )}

                  {repositories.length > 0 && (
                    <div className="recent-repository-list">
                      {repositories.slice(0, 4).map((repo) => (
                        <div className="recent-repository" key={repo.id}>
                          <span className="repo-avatar">
                            {repo.name.slice(0, 1).toUpperCase()}
                          </span>
                          <span className="recent-repo-copy">
                            <strong>{repo.name}</strong>
                            <small>
                              {repo.description || 'No description provided'}
                            </small>
                          </span>
                          <span
                            className={`source-badge ${repo.sourceType.toLowerCase() === 'local'
                                ? 'source-local'
                                : 'source-remote'
                              }`}
                          >
                            {repo.sourceType}
                          </span>
                        </div>
                      ))}
                    </div>
                  )}
                </section>
              </div>

              {/* Workspace tools */}
              <section className="card">
                <SectionHeading
                  eyebrow="EXPLORE CODEATLAS"
                  title="Your workspace tools"
                  description="Start with the capabilities available in this build."
                />

                <div className="tool-grid">
                  <button
                    type="button"
                    className="tool-card"
                    onClick={() => setActiveSection('Repositories')}
                  >
                    <span className="tool-icon violet">⌘</span>
                    <strong>Repository management</strong>
                    <p>
                      Register repositories and review their source metadata.
                    </p>
                    <span className="tool-link">Open repositories →</span>
                  </button>

                  <button
                    type="button"
                    className="tool-card"
                    onClick={() => setActiveSection('Code Analysis')}
                  >
                    <span className="tool-icon blue">{'</>'}</span>
                    <strong>Folder inspection</strong>
                    <p>
                      Enumerate files, extension counts, and observed project
                      types.
                    </p>
                    <span className="tool-link">Inspect workspace →</span>
                  </button>

                  <button
                    type="button"
                    className="tool-card"
                    onClick={() => setActiveSection('Architecture')}
                  >
                    <span className="tool-icon amber">◇</span>
                    <strong>Architecture insights</strong>
                    <p>
                      Review the platform structure and planned next
                      capabilities.
                    </p>
                    <span className="tool-link">View architecture →</span>
                  </button>
                </div>
              </section>
            </>
          )}

          {/* REPOSITORIES */}
          {activeSection === 'Repositories' && (
            <>
              <section className="card">
                <SectionHeading
                  eyebrow="REPOSITORY MANAGEMENT"
                  title="Register a repository"
                  description="Add repository metadata to your current CodeAtlas workspace."
                />

                <div className="notice notice-info">
                  <strong>Current storage</strong>
                  <p>
                    Repository metadata is held in memory in this build and
                    resets when the API restarts.
                  </p>
                </div>

                <form onSubmit={handleCreateRepository}>
                  <div className="form-grid">
                    <div className="form-group">
                      <label className="form-label" htmlFor="repo-name">
                        Repository name <span className="required">*</span>
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
                      {formFieldErrors.Name && (
                        <p className="field-error">
                          {formFieldErrors.Name.join(' ')}
                        </p>
                      )}
                    </div>

                    <div className="form-group">
                      <label
                        className="form-label"
                        htmlFor="repo-source-type"
                      >
                        Source type <span className="required">*</span>
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
                      {formFieldErrors.SourceType && (
                        <p className="field-error">
                          {formFieldErrors.SourceType.join(' ')}
                        </p>
                      )}
                    </div>

                    <div className="form-group full-width">
                      <label className="form-label" htmlFor="repo-url">
                        Repository URL <span className="optional">Optional</span>
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
                      {formFieldErrors.RepositoryUrl && (
                        <p className="field-error">
                          {formFieldErrors.RepositoryUrl.join(' ')}
                        </p>
                      )}
                    </div>

                    <div className="form-group full-width">
                      <label className="form-label" htmlFor="repo-desc">
                        Description <span className="optional">Optional</span>
                      </label>
                      <textarea
                        id="repo-desc"
                        className="form-textarea"
                        placeholder="What does this repository contain?"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        maxLength={500}
                      />
                      {formFieldErrors.Description && (
                        <p className="field-error">
                          {formFieldErrors.Description.join(' ')}
                        </p>
                      )}
                    </div>
                  </div>

                  {formGeneralError && (
                    <p className="error-message">{formGeneralError}</p>
                  )}

                  {formSuccessMessage && (
                    <div className="success-message">
                      {formSuccessMessage}
                    </div>
                  )}

                  <div className="form-actions">
                    <button
                      type="submit"
                      className="primary-button"
                      disabled={formSubmitting}
                    >
                      {formSubmitting
                        ? 'Registering…'
                        : 'Register repository'}
                      <span aria-hidden="true">→</span>
                    </button>
                  </div>
                </form>
              </section>

              <section className="card">
                <SectionHeading
                  eyebrow="YOUR WORKSPACE"
                  title={`Registered repositories (${repositories.length})`}
                  description="Repository metadata currently available to CodeAtlas."
                  action={
                    <button
                      type="button"
                      className="refresh-button"
                      onClick={() => void fetchRepositories()}
                      disabled={reposLoading}
                    >
                      {reposLoading ? 'Refreshing…' : 'Refresh list'}
                    </button>
                  }
                />

                {reposLoading && repositories.length === 0 && (
                  <div className="inline-state">
                    <span className="spinner" />
                    Loading repositories…
                  </div>
                )}

                {reposError && (
                  <div className="notice notice-error">
                    <p>{reposError}</p>
                    <button
                      type="button"
                      className="text-button"
                      onClick={() => void fetchRepositories()}
                    >
                      Try again
                    </button>
                  </div>
                )}

                {!reposLoading &&
                  repositories.length === 0 &&
                  !reposError && (
                    <div className="empty-state">
                      <div className="empty-icon">⌘</div>
                      <strong>Your repository list is empty</strong>
                      <p>Use the form above to add your first repository.</p>
                    </div>
                  )}

                {repositories.length > 0 && (
                  <div className="table-wrap">
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
                              {repo.description && (
                                <div className="repo-desc">
                                  {repo.description}
                                </div>
                              )}
                            </td>
                            <td>
                              <span
                                className={`source-badge ${repo.sourceType.toLowerCase() === 'local'
                                    ? 'source-local'
                                    : 'source-remote'
                                  }`}
                              >
                                {repo.sourceType}
                              </span>
                            </td>
                            <td>
                              {new Date(repo.createdAtUtc).toLocaleDateString()}
                            </td>
                            <td>
                              {repo.repositoryUrl ? (
                                <a
                                  href={repo.repositoryUrl}
                                  target="_blank"
                                  rel="noreferrer noopener"
                                >
                                  View source ↗
                                </a>
                              ) : (
                                <span className="muted">—</span>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>
            </>
          )}

          {/* CODE ANALYSIS */}
          {activeSection === 'Code Analysis' && (
            <>
              <section className="card">
                <SectionHeading
                  eyebrow="READ-ONLY INSPECTION"
                  title="Inspect a local folder"
                  description="Review files and project signals within the configured workspace boundary."
                />

                <div className="notice notice-info">
                  <strong>Safe by design</strong>
                  <p>
                    This feature enumerates files and extensions. It does not
                    execute or modify repository code.
                  </p>
                </div>

                <form onSubmit={handleInspectFolder}>
                  <div className="form-group">
                    <label className="form-label" htmlFor="inspect-path">
                      Directory path{' '}
                      <span className="optional">
                        Relative to workspace root
                      </span>
                    </label>

                    <div className="input-action-row">
                      <input
                        id="inspect-path"
                        type="text"
                        className="form-input"
                        placeholder=". or a subfolder such as src"
                        value={inspectPath}
                        onChange={(e) => setInspectPath(e.target.value)}
                      />
                      <button
                        type="submit"
                        className="primary-button"
                        disabled={inspectLoading}
                      >
                        {inspectLoading ? 'Inspecting…' : 'Inspect folder'}
                        <span aria-hidden="true">→</span>
                      </button>
                    </div>
                  </div>
                </form>

                {inspectLoading && (
                  <div className="inline-state">
                    <span className="spinner" />
                    Inspecting {inspectPath}…
                  </div>
                )}

                {inspectError && (
                  <div className="notice notice-error">
                    <strong>Inspection failed</strong>
                    <p>{inspectError}</p>
                  </div>
                )}
              </section>

              {inspectionResult && (
                <>
                  <div className="metric-grid inspection-metrics">
                    <article className="metric-card">
                      <div className="metric-top">
                        <span className="metric-icon blue">{'</>'}</span>
                        <span className="metric-caption">INSPECTED</span>
                      </div>
                      <div className="metric-value">
                        {inspectionResult.totalFiles}
                      </div>
                      <div className="metric-label">Files discovered</div>
                    </article>

                    <article className="metric-card">
                      <div className="metric-top">
                        <span className="metric-icon violet">#</span>
                        <span className="metric-caption">FILE TYPES</span>
                      </div>
                      <div className="metric-value">
                        {Object.keys(inspectionResult.extensionCounts).length}
                      </div>
                      <div className="metric-label">Unique extensions</div>
                    </article>

                    <article className="metric-card">
                      <div className="metric-top">
                        <span className="metric-icon green">◇</span>
                        <span className="metric-caption">PROJECT</span>
                      </div>
                      <div className="metric-value metric-value-small">
                        {inspectionResult.projectClassifications.length || '—'}
                      </div>
                      <div className="metric-label">
                        Observed classifications
                      </div>
                    </article>
                  </div>

                  <section className="card">
                    <SectionHeading
                      eyebrow="INSPECTION SUMMARY"
                      title="Workspace overview"
                      description={`Results for ${inspectionResult.inspectedPath}`}
                    />

                    <div className="details-grid">
                      <div className="detail-item">
                        <span className="detail-label">Inspected path</span>
                        <span className="detail-value">
                          {inspectionResult.inspectedPath}
                        </span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Total files</span>
                        <span className="detail-value">
                          {inspectionResult.totalFiles}
                        </span>
                      </div>
                    </div>

                    <div className="module-area">
                      <span className="detail-label">
                        Observed project classifications
                      </span>
                      <div className="modules-list">
                        {inspectionResult.projectClassifications.length > 0 ? (
                          inspectionResult.projectClassifications.map(
                            (classification) => (
                              <span key={classification} className="class-badge">
                                {classification}
                              </span>
                            ),
                          )
                        ) : (
                          <span className="muted">None observed.</span>
                        )}
                      </div>
                    </div>

                    <div className="module-area">
                      <span className="detail-label">
                        File counts by extension
                      </span>
                      <div className="modules-list">
                        {Object.entries(inspectionResult.extensionCounts)
                          .sort((a, b) => b[1] - a[1])
                          .map(([extension, count]) => (
                            <span key={extension} className="module-pill">
                              {extension}: <strong>{count}</strong>
                            </span>
                          ))}
                      </div>
                    </div>
                  </section>

                  <section className="card">
                    <SectionHeading
                      eyebrow="FILE INVENTORY"
                      title={`Discovered files (${inspectionResult.relativeFilePaths.length})`}
                      description="Paths returned by the read-only folder inspection."
                    />

                    <div className="file-list-box">
                      {inspectionResult.relativeFilePaths.length > 0 ? (
                        inspectionResult.relativeFilePaths.map((path) => (
                          <div key={path} className="file-list-item">
                            <span className="file-icon">▤</span>
                            {path}
                          </div>
                        ))
                      ) : (
                        <div className="empty-state">
                          No files found in this directory.
                        </div>
                      )}
                    </div>
                  </section>
                </>
              )}
            </>
          )}

          {/* ARCHITECTURE */}
          {activeSection === 'Architecture' && (
            <>
              <section className="card">
                <SectionHeading
                  eyebrow="CURRENT DESIGN"
                  title="CodeAtlas platform architecture"
                  description="A modular monolith designed to keep feature boundaries clear while the product grows."
                />

                <div className="architecture-flow">
                  <div className="architecture-node architecture-frontend">
                    <span className="architecture-node-icon">◫</span>
                    <strong>React + TypeScript</strong>
                    <small>Frontend · Vite</small>
                  </div>

                  <div className="architecture-connector">
                    <span>HTTP / JSON</span>
                    <i />
                  </div>

                  <div className="architecture-node architecture-api">
                    <span className="architecture-node-icon">◇</span>
                    <strong>ASP.NET Core API</strong>
                    <small>.NET 10 · Modular monolith</small>
                  </div>

                  <div className="architecture-connector vertical-connector">
                    <span>Module boundaries</span>
                    <i />
                  </div>

                  <div className="architecture-modules">
                    {[
                      'Identity',
                      'Repositories',
                      'CodeAnalysis',
                      'Architecture',
                      'AiAssistant',
                      'Jobs',
                    ].map((moduleName) => (
                      <div className="architecture-module" key={moduleName}>
                        <span className="module-node-dot" />
                        <span>{moduleName}</span>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="notice notice-info architecture-note">
                  <strong>Current implementation note</strong>
                  <p>
                    The repository store is in-memory, and folder inspection is
                    read-only. This diagram shows the high-level platform
                    structure; it does not imply that AI analysis or dependency
                    visualization is already implemented.
                  </p>
                </div>
              </section>

              <section className="card">
                <SectionHeading
                  eyebrow="FUTURE CAPABILITIES"
                  title="Planned analysis workspace"
                  description="These are product directions, not active operations in the current build."
                />

                <div className="tool-grid">
                  <article className="tool-card planned-card">
                    <span className="tool-icon violet">⌘</span>
                    <strong>Code relationships</strong>
                    <p>
                      Connect types, methods, and project boundaries using
                      source analysis.
                    </p>
                    <span className="planned-label">Planned</span>
                  </article>

                  <article className="tool-card planned-card">
                    <span className="tool-icon blue">◇</span>
                    <strong>Architecture diagrams</strong>
                    <p>
                      Visualize component relationships with evidence from the
                      repository.
                    </p>
                    <span className="planned-label">Planned</span>
                  </article>

                  <article className="tool-card planned-card">
                    <span className="tool-icon amber">✳</span>
                    <strong>AI explanations</strong>
                    <p>
                      Answer codebase questions with source references and line
                      citations.
                    </p>
                    <span className="planned-label">Planned</span>
                  </article>
                </div>
              </section>
            </>
          )}
        </main>

        <footer className="workspace-footer">
          <span>
            CodeAtlas <span className="footer-separator">·</span> Code
            intelligence workspace
          </span>
          <span>Local development build</span>
        </footer>
      </div>
    </div>
  );
}
