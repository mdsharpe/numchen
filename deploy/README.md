# Deploying Numchen

The app runs as two pods (API + UI) in Kubernetes, fronted by a
Cloudflare Tunnel. Target environment is a single-node **k3s** cluster
on an Ubuntu server, but any Kubernetes cluster will do.

```
┌─────────────────────┐   WS/HTTPS   ┌──────────────────┐
│  Cloudflare edge    │ ───────────▶ │  cloudflared pod │
│ numchen.mdsharpe.com│              │ (outbound tunnel)│
└─────────────────────┘              └────────┬─────────┘
                                              │ cluster DNS
                              ┌───────────────┴──────────────┐
                              ▼                              ▼
                   ┌────────────────────┐        ┌────────────────────┐
                   │ Service: numchen-ui│        │ Service: numchen-api│
                   │   nginx SPA :80    │        │  ASP.NET Core :8080 │
                   └────────────────────┘        └────────────────────┘
```

Path routing happens inside cloudflared (see
`cloudflared/configmap.yaml`): `/hub/*` → API, everything else → UI.

## Layout

| Path | What |
| --- | --- |
| `helm/numchen/` | Portable Helm chart: API + UI Deployments and ClusterIP Services. No ingress, no tunnel — any cluster can install it. |
| `cloudflared/` | Deployment-specific manifests for this project's Cloudflare Tunnel. Apply alongside the chart if you want the same public entry point. |

## One-time server setup

### 1. Install k3s

The tunnel makes host ports 80/443 unnecessary, and the flags below
skip Traefik and the built-in service LB so k3s never binds host
ports — leaves any Podman containers alone.

```bash
curl -sfL https://get.k3s.io | \
  INSTALL_K3S_EXEC="--disable=traefik --disable=servicelb --write-kubeconfig-mode=644" \
  sh -
```

The installer creates a systemd unit, installs `kubectl`/`crictl`/`ctr`
symlinks, and writes an uninstall script to
`/usr/local/bin/k3s-uninstall.sh`. k3s auto-upgrades only if you opt in
via the system-upgrade controller; the installer itself pins the
channel, not a version — re-run the command later to move to a new
release.

Verify:

```bash
sudo systemctl status k3s
kubectl get nodes
```

Copy the kubeconfig if you want to drive it from elsewhere (e.g. over
Tailscale):

```bash
sudo cat /etc/rancher/k3s/k3s.yaml
# replace 127.0.0.1 with the server's Tailscale IP in the client copy
```

### 2. Install Helm

Helm isn't packaged in Ubuntu repos. Use the upstream install script:

```bash
curl -fsSL https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

### 3. GHCR pull secret (only needed while the package is private)

```bash
kubectl create namespace numchen
kubectl -n numchen create secret docker-registry ghcr \
  --docker-server=ghcr.io \
  --docker-username=<github-username> \
  --docker-password=<github-PAT-with-read:packages>
```

Then add `imagePullSecrets: [{ name: ghcr }]` via `--set` or an override file.
Skip this entirely once the images are public.

## Deploying the app

```bash
helm upgrade --install numchen deploy/helm/numchen \
  --namespace numchen --create-namespace \
  --set api.image.tag=latest \
  --set ui.image.tag=latest
```

Pin a specific SHA in production:

```bash
helm upgrade --install numchen deploy/helm/numchen -n numchen \
  --set api.image.tag=sha-<commit> \
  --set ui.image.tag=sha-<commit>
```

Sanity check:

```bash
kubectl -n numchen get pods
kubectl -n numchen port-forward svc/numchen-ui 8080:80
# browse to http://localhost:8080
```

## Exposing via Cloudflare Tunnel

See [`cloudflared/README.md`](cloudflared/README.md). Short version:

```bash
cloudflared login
cloudflared tunnel create numchen
cloudflared tunnel route dns numchen numchen.mdsharpe.com

kubectl -n numchen create secret generic cloudflared-credentials \
  --from-file=credentials.json=$HOME/.cloudflared/<UUID>.json

kubectl apply -f deploy/cloudflared/
```

## Updating

Images are built and pushed to GHCR by `.github/workflows/build.yml`
on every push to `main`, tagged `latest` and `sha-<commit>`.

To roll out the latest images:

```bash
kubectl -n numchen rollout restart deploy/numchen-api deploy/numchen-ui
```

(Because `imagePullPolicy: IfNotPresent` + `latest` can be cached,
pin to `sha-<commit>` via `helm upgrade` when you need determinism.)
