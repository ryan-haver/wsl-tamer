# Phase 12: Container Orchestration

Status: Vision (Long-term)
Timeline: Q3 2027
Priority: HIGH

## Quick Start

Detect a local Kubernetes (kind) cluster and view pods.

### Prerequisites

- `kubectl` installed in a WSL distro
- A local `kind` or `minikube` cluster (optional demo)

### Install kind and create a demo cluster (Ubuntu)

```powershell
wsl -d Ubuntu -- sh -lc 'curl -Lo ./kind https://kind.sigs.k8s.io/dl/v0.23.0/kind-linux-amd64 && chmod +x ./kind && sudo mv ./kind /usr/local/bin/kind && sudo apt -y install kubectl && kind create cluster'
```

### List pods and services

```powershell
wsl -d Ubuntu -- sh -lc 'kubectl get pods -A; kubectl get svc -A'
```

### Verify

- `kubectl get pods -A` returns system pods
- Services list returned without errors

## Overview

Advanced management of Kubernetes, Docker, and Podman inside WSL, including multi-cluster and multi-runtime control.

## 12.1 Advanced Kubernetes Management

Status: Planned
Complexity: Very High

- Kubernetes cluster detection
  - Detect k3s, k8s, kind, minikube in WSL
  - Multi-cluster support and context switching
- Cluster management UI
  - Visual cluster dashboard
  - Node status and health
  - Pod management (list, create, delete, logs)
  - Service and ingress management
  - ConfigMap and Secret management
  - Persistent volume management
- Deployment tools
  - YAML editor with validation
  - Helm chart browser and installer
  - Kustomize support
  - GitOps integration (ArgoCD, Flux)
- Monitoring & debugging
  - Pod resource usage
  - Pod logs viewer (search and filter)
  - Port forwarding UI
  - Execute commands in pods
  - Event viewer
  - Metrics dashboard (Prometheus integration)
- Multi-cluster operations
  - Deploy to multiple clusters
  - Cluster comparison
  - Failover configuration
  - Load balancing across clusters

## 12.2 Docker & Podman Advanced Management

Status: Planned
Complexity: High

- Docker management
  - Container lifecycle (list, start, stop, remove)
  - Image management (list, pull, push, build, tag)
  - Networks and volumes
  - Docker Compose UI
  - Multi-host Docker (Swarm)
- Podman management
  - Rootless container support
  - Pod management (Podman pods)
  - Systemd integration and Quadlet support
  - Podman Compose
- Container registry integration
  - Docker Hub, ghcr.io, quay.io, ECR, ACR, GCR
  - Registry authentication and image scanning
  - Registry mirroring
- Advanced features
  - Multi-architecture builds (buildx)
  - Layer cache management
  - Build secrets management
  - Container resource limits
  - Health checks and auto-restart
  - Log aggregation

### Technical Implementation

- Docker/Podman API integration: Docker.DotNet, Podman.Client
- Kubernetes API integration: Kubernetes.Client
- Runtime abstraction via `IContainerRuntime`
  - DockerRuntime, PodmanRuntime, ContainerdRuntime (future)

---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Design runtime abstraction layer (`IContainerRuntime`) | 8h | Critical | None | Planned |
| Implement DockerRuntime (Docker API) | 12h | High | Abstraction | Planned |
| Implement PodmanRuntime | 10h | High | Abstraction | Planned |
| Implement Kubernetes integration layer | 14h | High | Abstraction | Planned |
| Build multi-cluster context manager | 8h | High | K8s layer | Planned |
| Create OrchestrationDashboard UI | 12h | High | Services | Planned |
| Implement YAML editor + Helm/Kustomize tooling | 10h | Medium | K8s layer | Planned |
| Build Logs/Exec/Port forward panels | 8h | Medium | K8s layer | Planned |
| Implement Docker/Podman image + container UIs | 10h | Medium | Runtimes | Planned |
| Create registry integration + auth storage | 8h | Medium | Phase 1 cred service | Planned |
| Add monitoring hooks (Prometheus, metrics) | 8h | Medium | Phase 3 data | Planned |
| Implement multi-cluster deploy orchestrator | 8h | Medium | Context manager | Planned |
| Write unit tests (runtime mocks, API clients) | 12h | High | Services | Planned |
| Integration tests (kind/k3s, Docker, Podman) | 12h | High | Services | Planned |
| Manual validation across runtimes | 8h | High | Completion | Planned |
| Documentation + sample workflows | 5h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/ContainerRuntimeService.cs` – runtime abstraction
- `src/WslTamer.Core/Services/DockerRuntime.cs`
- `src/WslTamer.Core/Services/PodmanRuntime.cs`
- `src/WslTamer.Core/Services/KubernetesClusterService.cs`
- `src/WslTamer.Core/Services/ClusterContextService.cs`
- `src/WslTamer.Core/Services/OrchestrationDeploymentService.cs`
- `src/WslTamer.Core/Services/RegistryIntegrationService.cs`
- `src/WslTamer.Core/Services/HelmService.cs`
- `src/WslTamer.Core/Services/KustomizeService.cs`
- `src/WslTamer.Core/Services/ContainerLogsService.cs`
- `src/WslTamer.Core/Services/PortForwardService.cs`
- `src/WslTamer.Core/Models/ContainerRuntimeInfo.cs`
- `src/WslTamer.Core/Models/ClusterInfo.cs`
- `src/WslTamer.Core/Models/DeploymentPlan.cs`
- `src/WslTamer.Core/Models/RegistryCredential.cs`
- `src/WslTamer.Core/Models/HelmReleaseInfo.cs`
- `src/WslTamer.Core/Models/LogStreamSession.cs`
- `src/WslTamer.UI/Views/OrchestrationDashboardView.xaml`
- `src/WslTamer.UI/Views/ClusterDetailView.xaml`
- `src/WslTamer.UI/Views/YamlEditorView.xaml`
- `src/WslTamer.UI/Views/LogsPane.xaml`
- `src/WslTamer.UI/Views/RegistryManagerView.xaml`
- `src/WslTamer.UI/ViewModels/OrchestrationDashboardViewModel.cs`
- `src/WslTamer.UI/ViewModels/ClusterDetailViewModel.cs`
- `tests/WslTamer.Tests/Services/ContainerRuntimeServiceTests.cs`
- `tests/WslTamer.Tests/Services/KubernetesClusterServiceTests.cs`
- `tests/WslTamer.Tests/Services/HelmServiceTests.cs`

**Modified Files:**

- `src/WslTamer.Core/DependencyInjection.cs` – Register orchestration services
- `README.md` – Add orchestration capabilities
- `docs/roadmap/phase-07-packages.md` – Cross-reference registry integration

**New Classes/Interfaces:**

- `IContainerRuntimeService`
- `IDockerRuntime`
- `IPodmanRuntime`
- `IKubernetesClusterService`
- `IClusterContextService`
- `IOrchestrationDeploymentService`
- `IRegistryIntegrationService`
- `IHelmService`
- `IKustomizeService`
- `IContainerLogsService`
- `IPortForwardService`
- `ContainerRuntimeType` enum (Docker, Podman, Containerd)
- `ClusterType` enum (K8s, k3s, kind, Minikube)

### Testing Strategy

**Unit Tests:**

- Runtime detection and capability flags
- Docker/Podman command translation
- Kubernetes API calls (mocked) for pods/deployments
- YAML validation and Helm template rendering
- Registry authentication flows
- Port forwarding session lifecycle

**Integration Tests:**

- Interact with local kind cluster (create pod, view logs)
- Deploy Helm chart via orchestrator and verify resources
- Manage Docker containers (start/stop/logs) via runtime service
- Podman rootless container management scenario
- Multi-cluster context switch and deploy

**Manual Tests:**

- Spin up demo kind cluster and manage via dashboard
- Deploy sample app across two clusters
- Use YAML editor to edit deployment and apply changes
- Test Docker Compose UI and registry authentication
- Forward ports from pod to host and validate connectivity

### Migration/Upgrade Path

- New settings file `%APPDATA%\\WslTamer\\containers.json`
- Registry credentials stored via Credential Manager
- Optional runtime support; features hidden if runtimes unavailable
- Backward compatible with users not using containers
- Provide migration script to import existing kubeconfig contexts

### Documentation Updates

- Add "Container Orchestration" guide with walkthroughs
- Provide tutorials for Kubernetes, Docker, and Podman workflows
- Document registry authentication processes
- Include troubleshooting for kubeconfig, permissions, and networking
- Update README and release notes
