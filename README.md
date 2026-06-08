# Project Tracker - Task Management System

## Project Overview

Project Tracker is a comprehensive task management system built with **ASP.NET Core MVC**. It allows teams to manage projects, track work items, collaborate through comments, and monitor progress through an interactive dashboard.

## Technologies Used

| Technology | Version | Purpose |
|------------|---------|---------|
| **Backend** | ASP.NET Core 10.0 | MVC Framework |
| **Database** | SQL Server / LocalDB | Data storage |
| **ORM** | Entity Framework Core 10.0 | Database access |
| **Frontend** | Bootstrap 5, jQuery | UI framework |
| **Charts** | Chart.js | Data visualization |
| **Authentication** | ASP.NET Core Identity | User management |
| **Testing** | NUnit, Moq, Coverlet | Unit testing |
| **Version Control** | Git/GitHub | Source control |

## Features

### Core Features
- ✅ **User Authentication** - Register, login, logout with ASP.NET Core Identity
- ✅ **Role-Based Authorization** - Admin, ProjectManager, Developer, Tester, Viewer
- ✅ **Project Management** - Create, read, update, delete projects (soft delete)
- ✅ **Work Item Tracking** - Create tasks with status and priority
- ✅ **Team Management** - Add/remove team members with different roles
- ✅ **Comments System** - Add comments to work items
- ✅ **Interactive Dashboard** - Charts and statistics for project overview

### Advanced Features
- ✅ **Pagination** - 6 projects per page, 10 work items per page
- ✅ **Sorting & Filtering** - Sort by name, date, status; filter by status and priority
- ✅ **Search Functionality** - Search projects by name, work items by title
- ✅ **RESTful API** - JSON endpoints for projects and work items
- ✅ **AJAX** - Real-time status updates and comment posting
- ✅ **Custom Error Pages** - 401, 404, 500 error pages
- ✅ **Request Logging** - Middleware for logging HTTP requests
- ✅ **Unit Tests** - 70%+ code coverage with NUnit and Moq

