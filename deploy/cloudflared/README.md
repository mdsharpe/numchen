# Cloudflare Tunnel

Exposes the in-cluster `numchen-ui` and `numchen-api` Services at
`numchen.mdsharpe.com` via a Cloudflare Tunnel. No inbound ports
need to be opened on the host.

## One-off setup (on a machine with `cloudflared` installed)

```bash
cloudflared login                              # opens browser, picks the zone
cloudflared tunnel create numchen              # writes ~/.cloudflared/<UUID>.json
cloudflared tunnel route dns numchen numchen.mdsharpe.com
```

## Install into the cluster

Assumes the `numchen` namespace already exists (created by the Helm release).

```bash
# 1. Credentials secret (from the JSON written by `tunnel create`)
kubectl -n numchen create secret generic cloudflared-credentials \
  --from-file=credentials.json=$HOME/.cloudflared/<UUID>.json

# 2. ConfigMap + Deployment
kubectl apply -f deploy/cloudflared/
```

## Updating routes

Edit `configmap.yaml`, then:

```bash
kubectl apply -f deploy/cloudflared/configmap.yaml
kubectl -n numchen rollout restart deployment/cloudflared
```
