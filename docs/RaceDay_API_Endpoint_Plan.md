# RaceDay – API Endpoint Plan (Section B)

This document lists every API endpoint the RaceDay system exposes, in line with the ERD in Section A. Endpoints are grouped by functional area: Authentication, User Profile, Events, Categories, Event Enrolments, and Results.

**Authentication model:** the API uses cookie-based session authentication. `POST /api/auth/login` starts a session and the server sets a session cookie that identifies the user and their role on every later request. Role-based access is enforced on the server: "Organiser" endpoints reject Participants (403) and vice versa; protected endpoints reject anonymous callers (401). Organisers can only change data belonging to their own events.

**Part 3 additions** are marked **(Part 3)**: event distance and type, event filtering, the organiser dashboard feed, Azure Blob Storage image uploads (event banner and profile picture), enrolment status changes, and richer enrolment/result responses so the MVC front end needs no extra look-ups.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or a Participant. Passwords are stored hashed (PBKDF2), never in plain text. | None (public) | { fullName, email, password, role, phoneNumber } | 201 Created – user id, name, email, role<br>400 Bad Request – missing/invalid fields, password under 8 characters, or role not Organiser/Participant<br>409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and starts an authenticated session. The server sets a session cookie that identifies the user and their role on all subsequent requests. | None (public) | { email, password } | 200 OK – user id, name, email, role, profile picture URL; session cookie set<br>401 Unauthorized – invalid credentials |
| POST | /api/auth/logout | Ends the current session. | Any (logged in) | None | 200 OK – session cookie cleared<br>401 Unauthorized |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user, including their profile picture URL. | Any (logged in) | None | 200 OK – user profile<br>401 Unauthorized |
| GET | /api/users/{id} | Returns a specific user's profile. | Any (logged in) | None | 200 OK – user profile<br>404 Not Found |
| PUT | /api/users/{id} | Updates the logged-in user's own profile details. | Any (logged in, own profile only) | { fullName, phoneNumber, email } | 200 OK – updated profile<br>400 Bad Request<br>403 Forbidden – not own profile<br>404 Not Found<br>409 Conflict – email used by another account |
| POST | /api/users/me/profile-picture **(Part 3)** | Uploads the logged-in user's profile picture to Azure Blob Storage and saves its URL on the user. | Any (logged in) | multipart/form-data: file (JPG, PNG, WEBP or GIF, max 5 MB) | 200 OK – { url }<br>400 Bad Request – not an allowed image / too large<br>401 Unauthorized<br>502/503 – storage unavailable or not configured |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events **(Part 3: filters)** | Lists events, soonest first. Optional query parameters: `search`, `eventType` (Run/Walk/Cycle), `location`, `from`, `to`, `upcomingOnly=true`. | None (public) | None | 200 OK – array of events (name, date, location, description, distance, event type, banner URL, organiser)<br>400 Bad Request – unknown event type |
| GET | /api/events/mine **(Part 3)** | Returns the logged-in organiser's own events with enrolment totals and category counts (dashboard data). | Organiser | None | 200 OK – array of event summaries<br>401 Unauthorized<br>403 Forbidden |
| GET | /api/events/{id} | Returns full details of a single event. | None (public) | None | 200 OK – event details<br>404 Not Found |
| POST | /api/events | Creates a new event owned by the logged-in organiser. | Organiser | { eventName, eventDate, location, description, distanceKm, eventType } | 201 Created – new event<br>400 Bad Request – missing name/location, negative distance, or event type not Run/Walk/Cycle<br>401 / 403 |
| PUT | /api/events/{id} | Updates an event the organiser owns. | Organiser (own event only) | { eventName, eventDate, location, description, distanceKm, eventType } | 200 OK<br>400 Bad Request<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/events/{id} | Deletes an event the organiser owns (only if nobody has enrolled). | Organiser (own event only) | None | 204 No Content<br>403 Forbidden<br>404 Not Found<br>409 Conflict – event has enrolments |
| POST | /api/events/{id}/banner **(Part 3)** | Uploads (or replaces) the event banner image in Azure Blob Storage and saves its URL on the event. | Organiser (own event only) | multipart/form-data: file (JPG, PNG, WEBP or GIF, max 5 MB) | 200 OK – { url }<br>400 Bad Request – not an allowed image / too large<br>403 Forbidden<br>404 Not Found<br>502/503 – storage unavailable or not configured |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories (e.g. 5km, 10km, 21km, Under 20, Senior) for an event, including how many are enrolled. | None (public) | None | 200 OK – array of categories<br>404 Not Found |
| POST | /api/events/{eventId}/categories | Adds a new category to an event. | Organiser (own event only) | { categoryName, distanceKm, maxParticipants, price } | 201 Created – new category<br>400 Bad Request<br>403 Forbidden<br>404 Not Found |
| PUT | /api/categories/{id} | Updates an existing category. | Organiser (own event only) | { categoryName, distanceKm, maxParticipants, price } | 200 OK<br>400 Bad Request<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/categories/{id} | Removes a category from an event (only if nobody has enrolled in it). | Organiser (own event only) | None | 204 No Content<br>403 Forbidden<br>404 Not Found<br>409 Conflict – category has enrolments |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments | Enrols the logged-in participant into an event category. The enrolment links the participant, the event and the chosen category and is recorded immediately. | Participant | { eventId, categoryId } | 201 Created – enrolment (with event, category and participant details)<br>400 Bad Request – event already taken place<br>404 Not Found – event/category does not exist<br>409 Conflict – already enrolled or category full |
| GET | /api/enrolments/me | Lists the logged-in participant's own enrolments with event, category, status and any captured result. | Participant | None | 200 OK – array of enrolments |
| GET | /api/events/{eventId}/enrolments | Lists all enrolments for an event (participant, category, status, result). | Organiser (own event only) | None | 200 OK – array of enrolments<br>403 Forbidden<br>404 Not Found |
| PUT | /api/enrolments/{id}/status **(Part 3)** | Sets an enrolment's status to Confirmed, Pending or Cancelled. | Organiser (own event only) | { status } | 200 OK<br>400 Bad Request – invalid status<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/enrolments/{id} | Cancels the logged-in participant's own enrolment. | Participant (own enrolment only) | None | 204 No Content<br>403 Forbidden<br>404 Not Found<br>409 Conflict – a result has already been recorded |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/results | Captures a participant's finish time and finishing position for an enrolment. | Organiser (own event only) | { enrolmentId, finishTime, position, status } (status: Finished, DNF or DQ) | 201 Created – result record<br>400 Bad Request – invalid status, negative time, position below 1, or no finish time for a finisher<br>403 Forbidden<br>404 Not Found – enrolment does not exist<br>409 Conflict – result already captured |
| PUT | /api/results/{id} | Updates a previously captured result. | Organiser (own event only) | { finishTime, position, status } | 200 OK<br>400 Bad Request<br>403 Forbidden<br>404 Not Found |
| GET | /api/results/me **(Part 3: richer response)** | Lists the logged-in participant's race history: event name and date, category, finish time, finishing position and the number of finishers in that category (e.g. 47th of 312). | Participant | None | 200 OK – array of results |
| GET | /api/events/{eventId}/results | Lists all results for an event, ordered by category and position (public leaderboard). | None (public) | None | 200 OK – array of results (participant name, category, time, position, status)<br>404 Not Found |

---

**Note:** This plan covers, at minimum, Authentication, User Profile, Events, Categories, Event Enrolments, and Results as required. The endpoints marked **(Part 3)** were added or extended when the MVC front end and Azure Blob Storage integration were built; the implemented API matches this document.
