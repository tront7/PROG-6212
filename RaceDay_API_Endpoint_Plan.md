# RaceDay – API Endpoint Plan (Section B)

This document lists every API endpoint the RaceDay system will expose, in line with the ERD in Section A. Endpoints are grouped by functional area: Authentication, User Profile, Events, Categories, Event Enrolments, and Results.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or a Participant. | None (public) | { fullName, email, password, role } | 201 Created – new user id and role<br>400 Bad Request – missing/invalid fields<br>409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and issues an access token. | None (public) | { email, password } | 200 OK – JWT token and user role<br>401 Unauthorized – invalid credentials |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any (logged in) | None | 200 OK – user profile<br>401 Unauthorized |
| GET | /api/users/{id} | Returns a specific user's public profile. | Any (logged in) | None | 200 OK – user profile<br>404 Not Found |
| PUT | /api/users/{id} | Updates the logged-in user's own profile details. | Any (logged in, own profile only) | { fullName, phoneNumber, email } | 200 OK – updated profile<br>400 Bad Request<br>403 Forbidden – not own profile<br>404 Not Found |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events for participants to browse. | None (public) | None | 200 OK – array of events |
| GET | /api/events/{id} | Returns full details of a single event. | None (public) | None | 200 OK – event details<br>404 Not Found |
| POST | /api/events | Creates a new event owned by the logged-in organiser. | Organiser | { eventName, eventDate, location, description } | 201 Created – new event<br>400 Bad Request |
| PUT | /api/events/{id} | Updates an event the organiser owns. | Organiser (own event only) | { eventName, eventDate, location, description } | 200 OK – updated event<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/events/{id} | Deletes an event the organiser owns. | Organiser (own event only) | None | 204 No Content<br>403 Forbidden<br>404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories (e.g. 5km, 10km, 21km) for an event. | None (public) | None | 200 OK – array of categories<br>404 Not Found |
| POST | /api/events/{eventId}/categories | Adds a new category to an event. | Organiser (own event only) | { categoryName, distanceKm, maxParticipants, price } | 201 Created – new category<br>400 Bad Request<br>404 Not Found |
| PUT | /api/categories/{id} | Updates an existing category. | Organiser (own event only) | { categoryName, distanceKm, maxParticipants, price } | 200 OK – updated category<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser (own event only) | None | 204 No Content<br>403 Forbidden<br>404 Not Found |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments | Enrols the logged-in participant into an event category. | Participant | { eventId, categoryId } | 201 Created – enrolment record<br>400 Bad Request<br>404 Not Found – event/category does not exist<br>409 Conflict – already enrolled or category full |
| GET | /api/enrolments/me | Lists the logged-in participant's own enrolments. | Participant | None | 200 OK – array of enrolments |
| GET | /api/events/{eventId}/enrolments | Lists all enrolments for an event (for organiser management). | Organiser (own event only) | None | 200 OK – array of enrolments<br>403 Forbidden<br>404 Not Found |
| DELETE | /api/enrolments/{id} | Cancels the logged-in participant's own enrolment. | Participant (own enrolment only) | None | 204 No Content<br>403 Forbidden<br>404 Not Found |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/results | Captures a participant's result for an enrolment. | Organiser | { enrolmentId, finishTime, position, status } | 201 Created – result record<br>400 Bad Request<br>404 Not Found – enrolment does not exist<br>409 Conflict – result already captured |
| PUT | /api/results/{id} | Updates a previously captured result. | Organiser | { finishTime, position, status } | 200 OK – updated result<br>404 Not Found |
| GET | /api/results/me | Lists the logged-in participant's own results history. | Participant | None | 200 OK – array of results |
| GET | /api/events/{eventId}/results | Lists all results for an event (public leaderboard). | None (public) | None | 200 OK – array of results<br>404 Not Found |

---

**Note:** This plan covers, at minimum, Authentication, User Profile, Events, Categories, Event Enrolments, and Results as required. The implemented API in Part 2 will closely match this plan; any deviations will be explained in the README.
