# Authentication

Dashboard authentication uses ASP.NET Core Identity with secure password hashing, account lockout settings, unique emails, and confirmed email requirements.

Stage 1 login flow:

1. `POST /api/auth/login` validates email and password.
2. The API issues a short-lived JWT access token.
3. The API creates a hashed refresh-token session record.
4. The raw refresh token is returned and also written to an HTTP-only cookie.

Implemented endpoints:

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/sessions`
- `DELETE /api/auth/sessions/{id}`

Production must configure `JWT_SIGNING_KEY`. Development and Testing have guarded fallbacks only.
