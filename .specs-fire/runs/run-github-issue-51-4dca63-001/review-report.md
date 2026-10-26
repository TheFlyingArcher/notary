# Review Report: nginx-compose-service

Self-review of `deploy/nginx/default.conf`, `deploy/docker-compose.yml`, `deploy/.env.example`.

- No findings requiring change.
- Noted: `notary-web` no longer publishes 8080 on the host; anything that used `127.0.0.1:8080` on the Pi must go through NGINX on 443.
- Noted: nginx will fail to start until `fullchain.pem`/`privkey.pem` exist (expected; CA bootstrap runbook covers it).
- Noted: unmerged PR #62 also edits the notary-web block of the compose file; expect a small merge conflict.
