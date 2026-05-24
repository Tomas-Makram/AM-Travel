# AM Travel

<p align="center">
  <img src="https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?style=for-the-badge&logo=dotnet">
  <img src="https://img.shields.io/badge/C%23-.NET-239120?style=for-the-badge&logo=csharp">
  <img src="https://img.shields.io/badge/Entity_Framework-Core-6C3483?style=for-the-badge">
  <img src="https://img.shields.io/badge/SQL_Server-Database-CC2927?style=for-the-badge&logo=microsoftsqlserver">
  <img src="https://img.shields.io/badge/Bootstrap-UI-7952B3?style=for-the-badge&logo=bootstrap">
</p>

---

# 🚀 AM Travel

**AM Travel** is a professional travel office management system built with **ASP.NET Core MVC**, **Entity Framework Core**, and **SQL Server**.

The system is designed to manage real travel office operations including hotel bookings, transportation reservations, bus trips, seat layouts, company seat bookings, client payments, daily reports, accounting, and user management from one powerful centralized platform.

AM Travel is not just a basic booking application.  
It is a complete operational system built to handle real business workflows with accuracy, control, and scalability.

---

# 🎯 Project Vision

AM Travel aims to simplify and organize the daily workflow of travel agencies and transportation offices.

The system focuses on:

- Faster booking management
- Accurate seat control
- Clear financial tracking
- Organized daily operations
- Company transportation accounting
- Secure user access
- Reliable historical data
- Professional reporting

---

# ✨ Main Features

## 🏨 Hotel Booking Management

AM Travel supports complete hotel reservation workflows.

- Create hotel-only bookings.
- Create hotel + transportation bookings.
- Manage hotel name.
- Manage check-in and check-out dates.
- Calculate number of nights automatically.
- Manage room types.
- Manage number of rooms.
- Manage room night price.
- Calculate hotel total.
- Track children count.
- Support children under 6 years.
- Support children under 12 years.
- Display hotel reservation details clearly.

---

## 🚌 Transportation Booking Management

The transportation module allows the office to manage client transportation reservations with full seat control.

- Create transportation-only bookings.
- Create transportation with hotel bookings.
- Support departure trips.
- Support return trips.
- Support round trips.
- Select existing trips.
- Create new trips when needed.
- Select bus route.
- Select seats from trip layout.
- Track seat price.
- Calculate transportation total.
- Prevent duplicate seat reservation.
- Track reserved seats per trip.
- Manage From / To locations.
- Support route-based trip filtering.

---

## 💺 Advanced Seat Reservation System

AM Travel includes a seat reservation workflow designed for accuracy and conflict prevention.

- Seat selection per trip.
- Seat number tracking.
- Seat label tracking.
- Seat type tracking.
- Active and inactive seat support.
- Reserved seat tracking.
- Prevent duplicate client reservations.
- Prevent duplicate company reservations.
- Support return seat reservations.
- Support round-trip seat reservations.
- Seat snapshot support for company bookings.
- Conflict validation between clients and companies.

---

## 🧭 Bus & Route Management

The system provides full bus and route management for transportation operations.

- Create buses.
- Edit bus information.
- Activate and deactivate buses.
- Manage bus name.
- Manage plate number.
- Manage bus route.
- Configure layout rows.
- Configure layout columns.
- Store bus layout as JSON.
- Generate seats automatically from layout.
- Manage available seats.
- Manage inactive seats.

Supported seat types:

- Normal Seat
- VIP Seat
- Driver Seat
- Assistant Seat
- Aisle
- Door
- Bathroom
- Empty

---

## 🧠 Trip Snapshot System

One of the strongest parts of AM Travel is the **Trip Snapshot System**.

When a trip is created, the system stores important bus data as a snapshot.

Snapshot data includes:

- Bus name
- Plate number
- Seats count
- Layout data
- Seats data

This means that if the original bus is edited later, old trips remain accurate and unchanged.

This protects old reservations from being affected by future bus updates.

---

## 🏢 Company Seat Booking

AM Travel supports transportation company workflows.

The system allows companies to reserve seats or receive transferred client seats.

- Manage transportation companies.
- Create company seat bookings.
- Create company client bookings.
- Support inbound company bookings.
- Support outbound company bookings.
- Reserve internal seats for companies.
- Record external company seat reservations.
- Track company seat count.
- Track price per seat.
- Track company payments.
- View company account summary.
- Search company bookings.
- Filter company bookings.
- Manage company seat accounting.
- Manage company trip accounting.

---

## 🔁 Client Seat Transfer Workflow

AM Travel supports transferring client seats from an internal booking to an external transportation company.

This feature is useful when a travel office needs to move passengers from its own bus to another provider.

- Select seats from an existing booking.
- Transfer selected seats to a company.
- Store transferred seat snapshots.
- Store original booking reference.
- Track transferred seats count.
- Restore transferred seats when possible.
- Prevent restore if the seat was booked again after transfer.
- Keep the original booking and company accounting connected.

---

## 💳 Payment Management

AM Travel includes payment tracking for both clients and companies.

- Add booking payments.
- Add company seat payments.
- Add company trip payments.
- Track paid amount.
- Track remaining amount.
- Prevent overpayment.
- Store payment method.
- Store payment date.
- Store payment notes.
- Calculate booking financial status.

Supported payment methods:

- Cash
- Payment
- Vodafone Cash
- Etisalat Cash
- Orange Cash

---

## 📊 Daily Work Reports

AM Travel provides daily operational reports that help the team understand the daily workload clearly.

- Daily hotel bookings.
- Daily transportation trips.
- Passenger list per trip.
- Company seats per trip.
- Company clients per trip.
- Total bookings count.
- Total seats count.
- Total amount.
- Total remaining amount.
- Filter by selected date.
- Filter by hotel check-in date.
- Filter by hotel check-out date.
- Filter by transportation direction.

---

## 📈 Accounting & Reports

The system includes accounting features for clients, companies, and transportation trips.

- Booking payment tracking.
- Booking remaining balance tracking.
- Company seat accounting.
- Company trip accounting.
- Trip price tracking.
- Paid amount tracking.
- Remaining amount tracking.
- Export accounting data to Excel.
- Export accounting data to PDF.
- Generate booking reports.
- Generate booking tickets.
- View booking audit history.

---

## 👥 User & Role Management

AM Travel includes role-based user management.

Supported roles:

- Admin
- Helper
- Viewer

User management features:

- Create users.
- Activate users.
- Deactivate users.
- Block users.
- Unblock users.
- Change passwords.
- Track last login.
- Protect admin accounts.
- Manage account status.
- Control access by role.

---

## 🔐 Security Features

The project includes multiple security-focused features.

- Password hashing.
- Role-based authorization.
- Login validation.
- Failed login attempt tracking.
- Account blocking.
- Session management.
- Refresh token support.
- CSRF protection.
- Rate limiting.
- User throttling middleware.
- Security headers middleware.
- Data Protection support.
- Telegram OTP settings support.

> Sensitive values such as connection strings, JWT keys, Telegram bot tokens, certificates, and production secrets should not be committed to source control.

---

# ⚡ Key System Capabilities

✅ Hotel Booking Management  
✅ Transportation Booking Management  
✅ Round Trip Reservation Support  
✅ Advanced Bus Seat Layout System  
✅ Seat Conflict Prevention  
✅ Trip Snapshot Protection  
✅ Company Seat Booking  
✅ Client Seat Transfer Workflow  
✅ Booking Payment Tracking  
✅ Company Payment Tracking  
✅ Daily Work Reports  
✅ Excel Export  
✅ PDF Export  
✅ Booking Audit Logs  
✅ Role-Based Access Control  
✅ Secure Authentication Flow  
✅ Real Travel Office Workflow Support  

---

# 🧠 Technologies Used

| Technology | Purpose |
|---|---|
| ASP.NET Core MVC | Main Web Application Framework |
| C# | Backend Development |
| Entity Framework Core | ORM & Database Access |
| SQL Server | Relational Database |
| Razor Views | Server-Side UI Rendering |
| Bootstrap | Responsive User Interface |
| JavaScript | Frontend Interactions |
| jQuery | DOM Manipulation & AJAX |
| ClosedXML | Excel Export |
| QuestPDF | PDF Generation |
| Data Protection | Application Security |
| Rate Limiting | Request Protection |
| Telegram OTP | OTP Notification Support |

---

# 🏗️ Project Architecture

```text
AM Travel
│
├── Presentation Layer
│   ├── ASP.NET Core MVC
│   ├── Controllers
│   ├── Razor Views
│   ├── Static Files
│   └── Localization Resources
│
├── Business Layer
│   ├── Managers
│   ├── Services
│   ├── DTOs
│   ├── Filters
│   ├── Attributes
│   └── Business Workflows
│
└── Data Layer
    ├── Entity Models
    ├── DbContext
    ├── Relationships
    └── Database Configuration
```

---

# 📂 Project Structure

```text
AM-Travel/
│
├── AM Travel/
│   ├── Controllers/
│   │   ├── AccountController.cs
│   │   ├── AdminController.cs
│   │   ├── AuthController.cs
│   │   ├── BookingsController.cs
│   │   ├── DashboardController.cs
│   │   ├── TransportationController.cs
│   │   └── WorksController.cs
│   │
│   ├── Views/
│   │   ├── Account/
│   │   ├── Admin/
│   │   ├── Auth/
│   │   ├── Bookings/
│   │   ├── Dashboard/
│   │   ├── Transportation/
│   │   └── Works/
│   │
│   ├── wwwroot/
│   │   ├── css/
│   │   ├── js/
│   │   ├── img/
│   │   └── lib/
│   │
│   ├── Resources/
│   ├── Migrations/
│   ├── Program.cs
│   └── appsettings.json
│
├── BusinessLayer/
│   ├── DTOs/
│   │   ├── Account/
│   │   ├── Auth/
│   │   ├── Book/
│   │   ├── Bus/
│   │   ├── Company/
│   │   ├── CompanyBookSeat/
│   │   ├── Trip/
│   │   └── Work/
│   │
│   ├── Functions/
│   │   ├── AdminManager.cs
│   │   ├── AuthenticateManager.cs
│   │   ├── BookingManager.cs
│   │   ├── CompanySeatBookingManager.cs
│   │   ├── TransportationManager.cs
│   │   └── WorksManager.cs
│   │
│   ├── Services/
│   ├── Attributes/
│   ├── Filters/
│   └── Models/
│
├── DataLayer/
│   ├── Models/
│   │   ├── BookingData.cs
│   │   ├── BookingPayment.cs
│   │   ├── BookingRoom.cs
│   │   ├── BookingTransportationSeat.cs
│   │   ├── Bus.cs
│   │   ├── BusSeat.cs
│   │   ├── BusTrip.cs
│   │   ├── CompanySeatBooking.cs
│   │   ├── CompanySeatPayment.cs
│   │   ├── TransportationCompany.cs
│   │   ├── User.cs
│   │   └── Works.cs
│   │
│   └── DBContext.cs
│
├── AM Travel.slnx
├── README.md
└── .gitignore
```

---

# 🧩 Core Modules

## Booking Module

Handles client bookings, hotel details, transportation reservations, payments, tickets, reports, and audit logs.

## Transportation Module

Handles buses, layouts, trips, seats, routes, and trip availability.

## Company Seat Booking Module

Handles company reservations, company clients, external transportation bookings, and client transfers.

## Company Accounting Module

Handles company trip prices, paid amounts, remaining balances, and accounting exports.

## Daily Work Module

Displays daily hotel and transportation work in organized reports.

## User Management Module

Handles users, roles, activation, blocking, and password management.

## Authentication Module

Handles login, sessions, refresh tokens, password hashing, and account protection.

---

# 🗄️ Main Database Entities

| Entity | Purpose |
|---|---|
| User | Application users |
| UserSession | User login sessions |
| BookingData | Main booking record |
| BookingRoom | Hotel room details |
| BookingPayment | Client booking payments |
| BookingTransportationSeat | Client reserved seats |
| BookingAudit | Booking change tracking |
| BookingAuditDetail | Detailed audit fields |
| Bus | Bus information |
| BusSeat | Bus seat data |
| BusTrip | Transportation trip |
| TransportationCompany | External transportation companies |
| CompanySeatBooking | Company seat reservations |
| CompanySeatPayment | Company payments |
| Works | Daily work records |
| TelegramSettings | Telegram OTP configuration |

---

# 🔥 Why AM Travel Is Powerful

## Real Business Workflow

AM Travel is designed around real travel office scenarios, not simple demo CRUD screens.

## Accurate Seat Control

The system prevents duplicate seat reservations and keeps transportation availability accurate.

## Historical Data Protection

Trip snapshots protect old trips from being changed when the original bus is updated later.

## Flexible Booking Scenarios

Hotel-only, transportation-only, and combined bookings are supported.

## Company Integration

The system supports transportation companies, company clients, external bookings, and company accounting.

## Financial Accuracy

AM Travel tracks totals, discounts, paid amounts, remaining balances, company payments, and trip prices.

## Operational Clarity

Daily reports give the team a clear view of the work that must be handled every day.

## Auditability

Booking audit logs make important changes traceable and improve accountability.

## Secure Access

Role-based permissions protect sensitive operations and financial actions.

---

# 🖥️ Installation

## 1️⃣ Clone Repository

```bash
git clone https://github.com/Tomas-Makram/AM-Travel.git
cd AM-Travel
```

---

## 2️⃣ Configure Application Settings

Update your local `appsettings.json` or use environment variables for sensitive values.

Example:

```json
{
  "ConnectionStrings": {
    "Connection": "Server=YOUR_SERVER;Database=AMTravelDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "YOUR_LOCAL_JWT_SECRET_KEY",
    "Issuer": "AMTravel",
    "Audience": "AMTravel",
    "DurationInMinutes": 15
  },
  "TelegramSettings": {
    "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
    "ChatId": "YOUR_TELEGRAM_CHAT_ID"
  }
}
```

---

## 3️⃣ Apply Database Migrations

```bash
dotnet ef database update
```

---

## 4️⃣ Run Application

```bash
dotnet run --project "AM Travel/AM Travel.csproj"
```

---

# 🌐 Open In Browser

```text
https://localhost:5001
```

or

```text
http://localhost:5000
```

The exact URL may differ depending on your local launch settings.

---

# 📸 Screenshots

> Add screenshots here later.

```md
![Dashboard](docs/screenshots/dashboard.png)
![Booking Details](docs/screenshots/booking-details.png)
![Transportation Trips](docs/screenshots/transportation-trips.png)
![Company Accounting](docs/screenshots/company-accounting.png)
```

---

# 🛡️ Security Notice

Before pushing to GitHub, make sure no real secrets exist in:

- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Production.json`
- `Dockerfile`
- `docker-compose.yml`
- `.env`
- PFX certificates
- Data Protection key files
- Telegram bot tokens
- Database connection strings
- JWT secret keys

Recommended `.gitignore` entries:

```gitignore
.vs/
bin/
obj/

*.user
*.suo
*.rsuser
*.log

.env
appsettings.Development.json
appsettings.Production.json
appsettings.Local.json

*.pfx
*.pem
*.key
*.crt

AM Travel/App_Data/Keys/
```

---

# 🎯 Future Improvements

- REST API support
- Mobile application integration
- Real-time seat availability updates
- Advanced analytics dashboard
- Notification system
- Automated database backup
- Multi-branch office support
- Online customer booking portal
- Advanced permission management
- More export customization options
- WebSocket-based live updates
- Advanced financial dashboard

---

# 📈 System Goals

AM Travel aims to become a complete travel office management platform that combines booking, transportation, payments, companies, reports, and security in one reliable system.

The project focuses on:

- Operational speed
- Data accuracy
- Seat control
- Financial clarity
- Secure access
- Professional reporting
- Real-world travel office workflows

---

# 👨‍💻 Developer

Developed by **Tomas Makram**

---

# 📄 License

This project is proprietary software unless a license file is added.

---

# ⭐ Support

If you like this project, give it a ⭐ on GitHub.

---

# ✅ Final Statement

**AM Travel** is built to make travel office operations faster, clearer, and more reliable.

It brings hotel bookings, transportation trips, bus seat reservations, company accounting, payments, reports, and user management together in one professional system.
