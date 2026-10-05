# Multi-Tenant SaaS ASP.NET Core Web API Backend

A production-ready, enterprise-grade **Multi-Tenant SaaS Web API** built with **.NET 8 (LTS)** following **Clean Architecture**, **CQRS with MediatR**, **ASP.NET Core Identity**, **JWT Bearer Authentication with Refresh Tokens**, **EF Core Global Query Filters for Tenant Isolation**, and **Extensible Module-Based Authorization**.

---

## 1. Architectural Overview & Solution Structure

The project is structured according to **Clean Architecture** principles, maintaining strict separation of concerns and dependency inversion:

```
backend/
├── MultiTenantSaaS.sln
├── src/
│   ├── MultiTenantSaaS.Domain/             # Core Enterprise Entities, Enums, Interfaces (Zero external dependencies)
│   │   ├── Common/                         # BaseEntity, BaseAuditableEntity, IHasTenant
│   │   ├── Entities/                       # ApplicationUser, ApplicationRole, Tenant, Module, TenantModuleAccess, EmployeeModulePermission, RefreshToken, AuditLog
│   │   └── Enums/                          # UserRoles, SubscriptionStatus
│   │
│   ├── MultiTenantSaaS.Application/        # Application Business Logic & CQRS
│   │   ├── Common/                         # ApiResponse, Interfaces, Exceptions, Behaviors (ValidationBehavior), Mappings (AutoMapper)
│   │   ├── DTOs/                           # Auth, SuperAdmin, Admin, Employee DTOs
│   │   ├── Features/                       # CQRS Commands & Queries (Auth, SuperAdmin, Admin, Employee)
│   │   ├── Validators/                     # FluentValidation Request Validators
│   │   └── DependencyInjection.cs
│   │
│   ├── MultiTenantSaaS.Infrastructure/     # Persistence, Identity, and External Services
│   │   ├── Persistence/                    # ApplicationDbContext, Entity Configurations, EF Migrations, DbContext Seeder
│   │   ├── Identity/                       # IdentityService, TokenService, CurrentUserService
│   │   ├── Services/                       # AuditService
│   │   └── DependencyInjection.cs
│   │
│   └── MultiTenantSaaS.API/                # Presentation Layer (ASP.NET Core Web API)
│       ├── Controllers/                    # Auth, SuperAdmin, Admin, Employee, Users, Inventory (Sample Module)
│       ├── Authorization/                  # [HasModuleAccess], ModuleAccessRequirement, ModuleAccessAuthorizationHandler, ModuleAccessPolicyProvider
│       ├── Middleware/                     # ExceptionHandlingMiddleware
│       ├── appsettings.json
│       └── Program.cs                      # Host config, Serilog, Swagger JWT, Database Auto-migration & Seeding
│
└── tests/
    └── MultiTenantSaaS.UnitTests/          # xUnit Test Suite
        ├── Auth/                           # LoginCommandHandlerTests
        ├── Authorization/                  # ModuleAccessAuthorizationHandlerTests (Multi-level gating tests)
        └── Services/                       # TokenServiceTests (JWT claims & Refresh Token tests)
```

---

## 2. Multi-Tenant Role Hierarchy & Module Access Model

The system implements a three-tier hierarchical tenancy and authorization model:

```
┌──────────────────────────────────────────────────────────────┐
│                         SuperAdmin                           │
│  • Full system platform access                               │
│  • Creates & manages Tenants (Admins)                        │
│  • Creates platform Modules & grants modules to Tenants      │
└──────────────────────────────┬───────────────────────────────┘
                               │ creates / enables modules
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                    Admin (Tenant / Company)                  │
│  • Owner of their Tenant organization                        │
│  • Accesses only Modules granted to their Tenant             │
│  • Creates and manages their own Employees                   │
│  • Assigns granular permissions to Employees                 │
└──────────────────────────────┬───────────────────────────────┘
                               │ creates / assigns permissions
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                           Employee                           │
│  • Belongs strictly to their Admin's Tenant                  │
│  • Accesses only Modules granted to Tenant AND explicitly    │
│    assigned by Admin (with View/Create/Edit/Delete rights)   │
└──────────────────────────────────────────────────────────────┘
```

### Module Authorization Check Pipeline

When a user calls an endpoint decorated with `[HasModuleAccess("Inventory", ModulePermissionType.Create)]`:

1. **SuperAdmin**: Bypasses all checks with full platform rights.
2. **Admin**:
   - Checks that the module is globally active (`Module.IsGlobalActive == true`).
   - Checks that SuperAdmin has enabled this module for the Admin's tenant (`TenantModuleAccess.IsEnabled == true`).
3. **Employee**:
   - Checks that the module is globally active.
   - Checks that the module is enabled for the employee's tenant.
   - Checks the employee's granular permission record (`EmployeeModulePermission.CanCreate == true`).

---

## 3. Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- SQL Server (LocalDB, Express, or Azure SQL) OR Docker SQL Server

### Database Configuration

Update the connection string in `src/MultiTenantSaaS.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MultiTenantSaaSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Jwt": {
    "SecretKey": "SuperSecretEnterpriseMultiTenantSaaSJwtSigningKey2026!WithSufficientLengthForHS256Bit",
    "Issuer": "MultiTenantSaaS",
    "Audience": "MultiTenantSaaS.Client",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

### Applying Migrations & Running

When you start the API, it **automatically creates the database and runs any pending EF Core migrations**, followed by seeding the default roles, default SuperAdmin, and sample modules.

To run both backend and frontend together:

```bash
# From the root repository directory:
npm run dev:all
# or
npm start
```

To run individual projects:

```bash
# Run Backend only:
npm run backend:run

# Run Frontend only:
npm run dev
```

Once started, navigate to:
- **Swagger UI**: `https://localhost:5001/` or `http://localhost:5000/`

---

## 4. Default Seeded Data & Test Accounts

### 1. Platform SuperAdmin
- **Email**: `superadmin@saas.com`
- **Password**: `SuperAdmin123!`
- **Role**: `SuperAdmin`
- **Access**: Full platform authority, can create/manage tenants and create/grant system modules.

### 2. Client Tenant & Admin (Acme Shipping & Logistics)
- **Email**: `admin@acme.com`
- **Password**: `AdminPassword123!`
- **Role**: `Admin`
- **Tenant**: `Acme Shipping & Logistics`
- **Enabled Modules**: `Inventory`, `Reports`, `CRM`
- **Access**: Can onboard employees and assign permissions on tenant-enabled modules.

### 3. Employee (Bob Martinez @ Acme)
- **Email**: `employee@acme.com`
- **Password**: `EmployeePassword123!`
- **Role**: `Employee`
- **Tenant**: `Acme Shipping & Logistics`
- **Assigned Permissions**:
  - `Inventory`: View, Create, Edit (Delete restricted)
  - `Reports`: View only (Create, Edit, Delete restricted)

### 4. Vessel Master (Capt. Edward Smith @ MV Oceanic Pioneer)
- **Email**: `master@oceanic.com`
- **Password**: `MasterPassword123!`
- **Role**: `VesselMaster`
- **Tenant**: `Acme Shipping & Logistics`
- **Assigned Vessel**: `MV OCEANIC PIONEER` (IMO: `9417878`)
- **Access**: Vessel Master Command & Performance Dashboard (`/vessel-master`), live noon report telemetry, fuel monitoring (VLSFO & LSMGO ROB/consumption), and offline report submission.

### Pre-Seeded Modules

- `Inventory` (Manage inventory, stock levels, warehouses, and tracking)
- `Payroll` (Process employee payroll, tax deductions, and payslips)
- `Reports` (Generate advanced business analytics and financial reports)
- `CRM` (Customer relationship management and sales lead tracking)

---

## 5. API Endpoints Reference

All API responses follow a uniform JSON structure:

```json
{
  "success": true,
  "message": "Operation description",
  "data": { ... },
  "errors": null
}
```

### Authentication (`/api/auth`)

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/auth/login` | Login with Email & Password. Returns JWT Access Token + Refresh Token | Anonymous |
| `POST` | `/api/auth/refresh-token` | Generate new Access Token using Refresh Token | Anonymous |
| `POST` | `/api/auth/logout` | Revoke current or all active refresh tokens | Bearer Token |

### SuperAdmin (`/api/superadmin`)

*Requires `[Authorize(Roles = "SuperAdmin")]`*

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/superadmin/admins` | Create a new Tenant & Admin account (with optional initial modules) |
| `GET` | `/api/superadmin/admins` | List all Tenants / Admins across the platform |
| `PUT` | `/api/superadmin/admins/{id}/status` | Activate/deactivate Tenant or change Subscription Status |
| `GET` | `/api/superadmin/modules` | List all platform modules |
| `POST` | `/api/superadmin/modules` | Create a new extensible platform module |
| `POST` | `/api/superadmin/tenant-module-access` | Grant or revoke a module for a specific Tenant |
| `GET` | `/api/superadmin/tenant-module-access/{tenantId}` | View assigned modules for a specific Tenant |

### Admin (`/api/admin`)

*Requires `[Authorize(Roles = "Admin")]`*

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/admin/employees` | Create a new Employee under Admin's Tenant |
| `GET` | `/api/admin/employees` | List all Employees belonging to Admin's Tenant |
| `PUT` | `/api/admin/employees/{id}` | Update Employee details |
| `DELETE` | `/api/admin/employees/{id}` | Deactivate Employee and revoke active tokens |
| `GET` | `/api/admin/modules` | List modules enabled for Admin's Tenant by SuperAdmin |
| `POST` | `/api/admin/employee-permissions` | Assign module permissions (View, Create, Edit, Delete) to an Employee |

### Employee (`/api/employee`)

*Requires `[Authorize(Roles = "Employee,Admin,SuperAdmin")]`*

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/employee/my-modules` | List modules and permissions assigned to logged-in employee |

### Shared Profile (`/api/users`)

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/users/me` | Get profile details of the current logged-in user | Bearer Token |

### Module 1: Fleet & Voyages (`/api/voyages` & `/api/voyage-orders`)

*Scoped by Tenant Global Query Filters*

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/voyages` | List all active/historical voyages for caller's Tenant | Bearer Token |
| `GET` | `/api/voyages/{idOrCode}` | Get single voyage particulars & passage legs by ID or Code | Bearer Token |
| `POST` | `/api/voyages` | Create new voyage under caller's Tenant | Bearer Token |
| `GET` | `/api/voyage-orders` | List voyage orders for caller's Tenant | Bearer Token |
| `POST` | `/api/voyage-orders` | Create new voyage order under caller's Tenant | Bearer Token |

### Module 2: Vessel Particulars & Master Registry (`/api/vessels`)

*Scoped by Tenant Global Query Filters with Field Change Audit History*

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/vessels` | List all vessels registered under caller's Tenant | Bearer Token |
| `GET` | `/api/vessels/{idOrImo}` | Get detailed particulars and historical change log of a vessel | Bearer Token |
| `POST` | `/api/vessels` | Register a new vessel under caller's Tenant fleet | Bearer Token |
| `PUT` | `/api/vessels/{id}` | Update vessel particulars & auto-record field change history | Bearer Token |
| `DELETE` | `/api/vessels/{id}` | Deactivate a vessel from tenant fleet | Bearer Token |

### Module 3: Chartering & Voyage Estimation (`/api/chartering`)

*Scoped by Tenant Global Query Filters*

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/chartering/estimates` | List all saved voyage estimates for caller's Tenant | Bearer Token |
| `POST` | `/api/chartering/estimates` | Save or update a voyage estimation calculation snapshot | Bearer Token |
| `DELETE` | `/api/chartering/estimates/{id}` | Delete a saved voyage estimate | Bearer Token |
| `GET` | `/api/chartering/books/cargo` | List all entries in the Cargo Book | Bearer Token |
| `GET` | `/api/chartering/books/tonnage` | List all open vessels in the Tonnage Book | Bearer Token |

### Module 4: Bunker Management & Fuel Inventory (`/api/bunker`)

*Scoped by Tenant Global Query Filters*

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/bunker/requirements` | List all bunker fuel procurement cycles and requirements | Bearer Token |
| `GET` | `/api/bunker/requirements/{idOrNo}` | Get single bunker procurement requirement by ID or code | Bearer Token |
| `POST` | `/api/bunker/requirements` | Raise a new bunker procurement requirement | Bearer Token |
| `PUT` | `/api/bunker/requirements/{id}` | Update bunker lifecycle stage, quote selection, BDN or invoice | Bearer Token |
| `DELETE` | `/api/bunker/requirements/{id}` | Delete a bunker requirement | Bearer Token |

### Module 5: Emissions & Environmental Compliance (`/api/emissions`)

*Scoped by Tenant Global Query Filters*

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/emissions` | List all emissions and regulatory compliance records | Bearer Token |
| `GET` | `/api/emissions/{voyageCode}` | Get emissions compliance doc, EU ETS, CII rating & FuelEU status | Bearer Token |
| `POST` | `/api/emissions` | Save, audit or approve emissions compliance document | Bearer Token |

### Module 6: Accounts, Invoicing & Financial Ledger (`/api/accounts`)

*Scoped by Tenant Global Query Filters*

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/accounts/transactions` | List all financial ledger transactions (Payables & Receivables) | Bearer Token |
| `GET` | `/api/accounts/transactions/{idOrNo}` | Get transaction record by ID or Transaction Number | Bearer Token |
| `POST` | `/api/accounts/transactions` | Create a transaction (Hire, Freight, Bunker, PDA, FDA, etc.) | Bearer Token |
| `PUT` | `/api/accounts/transactions/{id}` | Update transaction workflow state, approval, settlement or reconciliation | Bearer Token |

### Module 7: Vessel Reporting & Telemetry (`/api/vessel-reports`)

*Scoped by Tenant Global Query Filters & Vessel Scoping*

| Method | Route | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/vessel-reports` | List all vessel noon and event reports (Filterable by `?imo=` and `?voyageCode=`) | Bearer Token |
| `GET` | `/api/vessel-reports/{idOrNo}` | Get single report telemetry by ID or Report Number | Bearer Token |
| `POST` | `/api/vessel-reports` | Submit a vessel report (telemetry, noon logs, fuel consumptions/ROB) | Bearer Token |

### Sample Feature Controller (`/api/inventory`)

*Demonstrates real-world module authorization enforcement*

| Method | Route | Required Permission |
|---|---|---|
| `GET` | `/api/inventory` | `[HasModuleAccess("Inventory", ModulePermissionType.View)]` |
| `POST` | `/api/inventory` | `[HasModuleAccess("Inventory", ModulePermissionType.Create)]` |
| `PUT` | `/api/inventory/{id}` | `[HasModuleAccess("Inventory", ModulePermissionType.Edit)]` |
| `DELETE` | `/api/inventory/{id}` | `[HasModuleAccess("Inventory", ModulePermissionType.Delete)]` |

---

## 6. Running Unit Tests

To run the xUnit test suite covering JWT authentication, authorization handlers, and CQRS handlers:

```bash
cd backend
dotnet test
```
