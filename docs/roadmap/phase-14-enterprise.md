# Phase 14: Enterprise Security & Secrets

Status: Vision (Long-term)
Timeline: Q1 2028
Priority: HIGH

## Quick Start

Store and retrieve a secret using Windows Credential Manager (demo).

### Prerequisites

- Windows Credential Manager available

### Save a secret (PowerShell)

```powershell
$target = 'WSLTamer:Example:ApiToken'
$user   = 'user'
$secret = 'supersecret'

$vault = [Windows.Security.Credentials.PasswordVault]::new()
$cred  = [Windows.Security.Credentials.PasswordCredential]::new($target, $user, $secret)
$vault.Add($cred)
```

### Retrieve a secret

```powershell
$vault = [Windows.Security.Credentials.PasswordVault]::new()
$item  = $vault.Retrieve('WSLTamer:Example:ApiToken','user')
$item.RetrievePassword()
$item.Password
```

### Verify

- Secret is stored and retrieved successfully
- Avoid printing secrets in production; demo for Quick Start only

## Overview

Enterprise-grade secrets management, PowerShell remoting, and comprehensive credential lifecycle features for secure, compliant operations.

## 14.1 Advanced Secrets Management

Status: Planned
Complexity: Very High

- Secrets vault integration
  - HashiCorp Vault (dynamic secrets, PKI)
  - Azure Key Vault (managed identities, RBAC)
  - AWS Secrets Manager (rotation, replication)
  - 1Password / Bitwarden (CLI integration, secret injection)
- Local secrets management
  - Encrypted storage (AES-256), Windows DPAPI
  - HSM support, secrets rotation
  - Access control per distro
- Environment variable injection
  - Inject secrets as env vars
  - Temporary/auto-expiring secrets
  - Templates with masked display in UI
- Certificate management
  - X.509 storage and private keys
  - CSRs, Let's Encrypt automation
  - Renewal alerts and mTLS configuration

## 14.2 PowerShell Remoting Integration

Status: Planned
Complexity: Medium

- PSRemoting to WSL via SSH
  - Install PowerShell (pwsh) in WSL
  - Configure SSH-based remoting (or WinRM alternative)
  - Enter-PSSession/New-PSSession support
- Remote session management
  - Create/close sessions, interactive UI
  - Persistent sessions and reconnection
  - Multiple concurrent sessions
- Workflow automation
  - Execute PowerShell scripts remotely
  - Copy files via PS sessions
  - Parallel execution across distros
  - Error handling and logging
- Credential management for PSRemoting
  - Secure credential storage
  - Certificate-based and SSH key auth
  - Kerberos support (domain environments)

## 14.3 Comprehensive Credential Management

Status: Enhancement to Phase 3.3
Complexity: High

- Unified credential store
  - Passwords, API keys, tokens, certificates
  - SSH/GPG keys, DB connection strings
  - Cloud provider credentials (AWS, Azure, GCP)
- Credential types and lifecycle
  - Generation, storage (encrypted), rotation
  - Expiration, revocation, audit trails
- Credential sharing & compliance
  - Share with team members (encrypted)
  - Temporary access grants, approval workflows
  - Compliance reporting (SOC 2, PCI-DSS, HIPAA)

---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Design SecretsPlatform abstraction (`ISecretProvider`) | 8h | Critical | Phase 3 cred service | Planned |
| Implement VaultProvider (dynamic secrets + PKI) | 14h | High | Abstraction | Planned |
| Implement AzureKeyVaultProvider (Managed Identity support) | 12h | High | Abstraction | Planned |
| Implement AwsSecretsProvider + rotation hooks | 12h | High | Abstraction | Planned |
| Build LocalEncryptedStore (DPAPI + optional HSM) | 10h | High | Abstraction | Planned |
| Create SecretsOrchestratorService (policy, rotation, masking) | 10h | High | Providers | Planned |
| Implement CertificateLifecycleService (CSR, renewal, alerts) | 8h | Medium | Phase 4 notifications | Planned |
| Extend CredentialStore UI with vault browser + injection flows | 12h | Medium | Services | Planned |
| Build PowerShellRemotingService (session pool, logging) | 10h | Medium | Phase 5 automation | Planned |
| Create RemotingSessionsView (UI) + CLI commands | 8h | Medium | Service | Planned |
| Implement CredentialSharingService (encryption + audit) | 8h | Medium | Phase 3 storage | Planned |
| Add compliance reporting exporter (SOC2/PCI templates) | 6h | Medium | Telemetry | Planned |
| Unit tests for providers, orchestrator, remoting | 12h | High | Services | Planned |
| Integration tests with Vault dev server + AKV emulator | 12h | High | Providers | Planned |
| Manual validation of secret injection + remoting workflows | 8h | High | Completion | Planned |
| Documentation + sample scripts (Vault, PS remoting) | 5h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Secrets/ISecretProvider.cs`
- `src/WslTamer.Core/Secrets/SecretMetadata.cs`
- `src/WslTamer.Core/Secrets/VaultSecretProvider.cs`
- `src/WslTamer.Core/Secrets/AzureKeyVaultProvider.cs`
- `src/WslTamer.Core/Secrets/AwsSecretsProvider.cs`
- `src/WslTamer.Core/Secrets/LocalEncryptedSecretProvider.cs`
- `src/WslTamer.Core/Services/SecretsOrchestratorService.cs`
- `src/WslTamer.Core/Services/CertificateLifecycleService.cs`
- `src/WslTamer.Core/Services/CredentialSharingService.cs`
- `src/WslTamer.Core/Services/ComplianceReportService.cs`
- `src/WslTamer.Core/Services/PowerShellRemotingService.cs`
- `src/WslTamer.Core/Models/SecretProviderType.cs`
- `src/WslTamer.Core/Models/SecretRotationPolicy.cs`
- `src/WslTamer.Core/Models/RemotingSessionInfo.cs`
- `src/WslTamer.UI/Views/SecretsDashboardView.xaml`
- `src/WslTamer.UI/Views/SecretDetailView.xaml`
- `src/WslTamer.UI/Views/RemotingSessionsView.xaml`
- `src/WslTamer.UI/ViewModels/SecretsDashboardViewModel.cs`
- `src/WslTamer.UI/ViewModels/SecretDetailViewModel.cs`
- `src/WslTamer.UI/ViewModels/RemotingSessionsViewModel.cs`
- `src/WslTamer.Cli/Commands/SecretCommand.cs`
- `src/WslTamer.Cli/Commands/RemotingCommand.cs`
- `tests/WslTamer.Tests/Secrets/VaultSecretProviderTests.cs`
- `tests/WslTamer.Tests/Secrets/AzureKeyVaultProviderTests.cs`
- `tests/WslTamer.Tests/Secrets/AwsSecretsProviderTests.cs`
- `tests/WslTamer.Tests/Services/SecretsOrchestratorServiceTests.cs`
- `tests/WslTamer.Tests/Services/PowerShellRemotingServiceTests.cs`

**Modified Files:**

- `src/WslTamer.Core/DependencyInjection.cs` – register secret providers + remoting service
- `src/WslTamer.Core/AppSettings.cs` – add secret provider + rotation policy configuration
- `src/WslTamer.Core/Security/CredentialManagerService.cs` – extend for sharing and auditing
- `README.md` – highlight enterprise secret/credential features
- `docs/roadmap/phase-03-foundations.md` – reference enhanced credential lifecycle

**New Classes/Interfaces:**

- `ISecretProvider`, `ISecretsOrchestratorService`
- `ICertificateLifecycleService`, `ICredentialSharingService`
- `IPowerShellRemotingService`
- `SecretProviderType` enum (Vault, AzureKeyVault, AwsSecrets, Local)
- `RemotingSessionState` enum (Connecting, Connected, Failed, Closed)

### Testing Strategy

**Unit Tests:**

- Provider credential acquisition + token refresh flows
- Secret masking + injection templates ensure no plaintext logs
- Certificate renewal logic handles pre-expiry thresholds
- Remoting session recovery + transcript logging behavior
- Compliance exporter builds accurate audit trails

**Integration Tests:**

- Run Vault dev server with PKI backend to validate dynamic secrets
- Use Azure Key Vault emulator/managed identity stub for CRUD + rotation
- Exercise AWS Secrets Manager via localstack for rotation workflows
- Establish PowerShell remoting into WSL distro through SSH, execute script, capture logs
- Verify credential sharing across two user profiles with DPAPI + public key exchange

**Manual Tests:**

- Configure Vault + Azure Key Vault and verify secret injection into env vars
- Rotate secrets with policy engine and confirm dependent services refresh
- Launch PS remoting sessions from UI/CLI, execute sample automation tasks
- Generate compliance report and validate exported artifacts (CSV/PDF)

### Migration/Upgrade Path

- New `%APPDATA%\\WslTamer\\secrets.json` for provider preferences and rotation policies
- `%APPDATA%\\WslTamer\\remoting-sessions.json` to persist remoting history (encrypted)
- Existing credentials auto-migrated to unified secrets schema via bootstrap script
- Vault/Azure/AWS credentials stored via Windows Credential Manager or DPAPI vault
- Feature flags guard enterprise modules; disabled for community editions

### Documentation Updates

- Publish "Enterprise Secrets" guide covering provider setup, rotation, injection
- Add "PowerShell Remoting" how-to with SSH + Kerberos walkthroughs
- Update compliance documentation with audit trail expectations
- Extend security FAQ with secret handling best practices and troubleshooting
- Cross-link Phase 11 remote management features for remoting dependencies
