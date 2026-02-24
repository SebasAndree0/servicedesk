# 🚀 NovaDesk (ServiceDesk)

## 🇺🇸 English

### 🧠 Description

**NovaDesk** is a ticket management and support system built with **ASP.NET Core**, designed for enterprise environments.

It allows organizations to manage incidents, requests, and internal support efficiently, including user management, SLA configuration, and auditing.

---

### 🏗️ Architecture

The project is divided into two main applications:

#### 🖥️ Frontend - `ServiceDesk.Web`

* ASP.NET Core MVC (Razor Views)
* Modern UI (Dark theme)
* Ticket management (create, edit, close, reopen)
* Admin panel
* Role-based authentication (Admin / Support)

#### ⚙️ Backend - `ServiceDesk.Api`

* ASP.NET Core Web API
* Clean Architecture
* Layer separation:

  * Application
  * Domain
  * Infrastructure
  * Services
  * Contracts (DTOs)
* Entity Framework Core
* RESTful API

---

### ✨ Features

* 🎫 Full ticket management
* 👥 User administration (CRUD)
* 🏷️ Priorities (P1, P2, P3)
* ⏱️ SLA configuration per priority
* 📊 Dashboard with metrics
* 🔄 Ticket states (Open, In Progress, Closed)
* 📎 Attachments / evidence
* 🧾 Deletion history (audit log)
* 🔐 Authentication & role management

---

### 🛠️ Technologies

* C# / .NET (ASP.NET Core)
* Entity Framework Core
* SQL Server / SQLite
* Razor Views (MVC)
* HTML / CSS / JavaScript

---

### ▶️ How to run

```bash
# Clone repository
git clone https://github.com/youruser/novadesk.git

# Backend
cd backend/ServiceDesk.Api
dotnet run

# Frontend
cd ../../web/ServiceDesk.Web
dotnet run
```

---

### 📌 Roadmap

* 🔔 Real-time notifications
* 📊 Advanced analytics dashboard
* 🤖 AI integration (auto replies)
* 🌐 Multi-tenant support

---

---

## 🇪🇸 Español

### 🧠 Descripción

**NovaDesk** es un sistema de gestión de tickets y soporte técnico desarrollado con **ASP.NET Core**, diseñado para entornos empresariales.

Permite gestionar incidencias, solicitudes y soporte interno de manera eficiente, con control de usuarios, SLA y auditoría.

---

### 🏗️ Arquitectura

El proyecto está dividido en dos aplicaciones principales:

#### 🖥️ Frontend - `ServiceDesk.Web`

* ASP.NET Core MVC (Razor Views)
* Interfaz moderna (Dark UI)
* Gestión de tickets (crear, editar, cerrar, reabrir)
* Panel administrativo
* Autenticación basada en roles (Admin / Soporte)

#### ⚙️ Backend - `ServiceDesk.Api`

* ASP.NET Core Web API
* Arquitectura en capas (Clean Architecture)
* Separación por:

  * Application
  * Domain
  * Infrastructure
  * Services
  * Contracts (DTOs)
* Entity Framework Core
* API RESTful

---

### ✨ Funcionalidades

* 🎫 Gestión completa de tickets
* 👥 Administración de usuarios (CRUD)
* 🏷️ Prioridades (P1, P2, P3)
* ⏱️ Configuración de SLA por prioridad
* 📊 Panel con métricas
* 🔄 Estados de tickets (Abierto, En progreso, Cerrado)
* 📎 Adjuntos / evidencia
* 🧾 Historial de borrados (auditoría)
* 🔐 Autenticación y control de roles

---

### 🛠️ Tecnologías

* C# / .NET (ASP.NET Core)
* Entity Framework Core
* SQL Server / SQLite
* Razor Views (MVC)
* HTML / CSS / JavaScript

---

### ▶️ Cómo ejecutar

```bash
# Clonar repositorio
git clone https://github.com/tuusuario/novadesk.git

# Backend
cd backend/ServiceDesk.Api
dotnet run

# Frontend
cd ../../web/ServiceDesk.Web
dotnet run
```

---

### 📌 Roadmap

* 🔔 Notificaciones en tiempo real
* 📊 Dashboard con gráficos avanzados
* 🤖 Integración con IA (auto-respuestas)
* 🌐 Multi-tenant (multi empresa)
