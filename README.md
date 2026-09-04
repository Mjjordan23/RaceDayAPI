# RaceDay API

## Student Information

**Name:** Murendeni Nethenzheni
**Student Number:** ST10396326
**Module:** PROG6212 – Programming 2B
**Assessment:** POE Part 1 – System Planning and Database



## Project Description

RaceDay is a full-stack event management platform designed for South African running, walking, and cycling events.

The system allows **Organisers** to create and manage sporting events, categories, participant enrolments, and race results. **Participants** can create accounts, browse available events, enter events by category, view their enrolments, and track their personal results.

Part 1 focuses on the system planning and database design before the API implementation.

---

## User Roles

### Organiser

Organisers can:

* Create events
* Edit events
* Delete events
* Manage event categories
* Capture participant results
* View all event enrolments

### Participant

Participants can:

* Create an account
* Browse available events
* Enter events by category
* View their own enrolments
* Track their personal results

---

## Technologies Used

* ASP.NET Core Web API
* SQL Server
* SQL Server Management Studio (SSMS)
* GitHub
* GitHub Actions

---

## Project Structure

```text
RaceDay API/
│
├── .github/
│   └── workflows/
│       └── part1-ci.yml
│
├── docs/
│   ├── RaceDay ERD.pdf
│   ├── API Endpoint Plan.pdf
│   └── RaceDayDB.sql
│
└── README.md
```

---

## Part 1 Documentation

The `docs` folder contains the planning and database documentation required for Part 1.

### ERD

The **RaceDay ERD** shows the database entities, attributes, primary keys, foreign keys, relationships, and cardinalities.

### API Endpoint Plan

The **API Endpoint Plan** defines the planned RESTful API endpoints, including HTTP methods, routes, descriptions, required roles, request bodies, and expected responses.

### SQL Database Script

The **RaceDayDB.sql** script creates the RaceDay database, tables, relationships, constraints, and sample data.

---

## Setup Instructions

### 1. Clone the Repository

Clone this repository from GitHub to your computer.

### 2. Open SQL Server Management Studio

Open **SQL Server Management Studio (SSMS)** and connect to your SQL Server instance.

### 3. Open the SQL Script

Open:

```text
docs/RaceDayDB.sql
```

### 4. Execute the Script

Run the script in SSMS to create the `RaceDayDB` database, tables, relationships, constraints, and sample data.

---

## GitHub Repository

Repository:

**[Add your GitHub repository link here]**

---

## CI/CD

GitHub Actions is used to validate the Part 1 repository structure and required documentation.

The successful GitHub Actions build is shown below:

**<img width="1882" height="955" alt="image" src="https://github.com/user-attachments/assets/57626631-9a23-4ed3-81de-9857a2368a01" />
**

---

## Presentation Video

The Part 1 presentation video demonstrates:

* The RaceDay system planning
* ERD design decisions
* API endpoint planning
* SQL database design
* Running the SQL script in SQL Server Management Studio

**[Add your unlisted YouTube presentation link here]**

---

## AI Usage Disclosure

Where AI tools were used during the development process, their use will be disclosed in accordance with the assessment requirements.
