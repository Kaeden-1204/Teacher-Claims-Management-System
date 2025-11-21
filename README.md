# PROG6212_Part2 – Teacher Claims Management System

## Purpose of the Program
The PROG6212_Part2 application is designed to streamline the process of submitting, verifying, and approving teacher claims while providing secure document management. The system ensures that teachers can submit claims, PC and AM roles can manage verification and approval, and HR can manage teacher profiles and generate invoices for approved claims.

---

## Features

- **Teacher Functionality:**  
  - Submit monthly claims with hours worked and subject details.  
  - Automatically calculate total payment based on hours worked and hourly rate.  
  - Upload supporting documents (PDF, Word, Excel, Image) with file size limits.  
  - View all submitted claims and track their status (Pending, Verified, Approved, Rejected).  
  - Display claim submission date automatically.  
  - Teachers can only view their own claims; the system ensures other lecturers' claims are not accessible.

- **PC (Programme Coordinator) Functionality:**  
  - Review submitted claims from teachers.  
  - Verify or reject claims after checking supporting documents.  

- **AM (Accounting Manager) Functionality:**  
  - Approve or reject claims that have been verified by PC.  

- **HR Functionality:**  
  - Add, edit, and delete teacher profiles (CRUD operations).  
  - View approved claims.  
  - Generate invoices for approved claims.  

- **Security & Validation:**  
  - Data validation for required fields and file uploads.  
  - Encrypted storage for uploaded documents using AES encryption.  
  - Session-based authentication ensures secure access per user role.  
  - Error handling for invalid file types and file size limits. Only allowed file types are accepted: PDF, Word, Excel, Images (JPG, PNG).  
  - Enhanced error logging and user messages for failed uploads or decryption issues.  

---

## Program Setup

1. **Clone the Repository:**  
   ```bash
   git clone <repository-url>
   ```

2. **Open in Visual Studio:**  
   - Open the solution file (`.sln`) in Visual Studio 2022 or later.  
   - Ensure **.NET 9 SDK** is installed.  

3. **Database Configuration:**  
   - Update the connection string in `appsettings.json` to point to your SQL Server instance.  
   - Example:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=ClaimDB;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
     ```

4. **Run Database Migrations:**  
   ```bash
   dotnet ef database update
   ```
   This will create the required tables: `Users`, `Claims`, and `ClaimDocuments`.

5. **Documents Folder:**  
   - The application automatically creates a `Documents` folder in the project root for storing encrypted files.  
   - Ensure the application has write permissions to this folder.

---
## Youtube Link
- https://youtu.be/5u8RCnpm0Zw
## How the Program Will Run

1. Start the application from Visual Studio (`F5`) or `dotnet run`.  
2. Login using one of the following roles: Teacher, PC, AM, or HR.  
3. **Teachers:** Submit claims, upload supporting documents, and calculate total payments.  
4. **PC:** Verify or reject submitted claims.  
5. **AM:** Approve or reject verified claims.  
6. **HR:** Add/edit/delete teacher profiles, view approved claims, and generate invoices.  
7. All claims are stored in the database with tracking for submission date, status, and associated documents.  
8. Teachers can only view their own claims; document access and downloads are restricted to claim owners.

---

## Lecturer Feedback Addressed

- **PDF Documents:**  
  - All uploaded PDF files can be securely stored, downloaded, and decrypted. Other supported file types (Word, Excel, Images) are also handled.  
  - Uploaded files are checked against allowed extensions and size limits to prevent unsupported files from being processed.  

- **Claim Privacy:**  
  - Teachers can only see their own claims. Claims from other lecturers are not accessible, as enforced in the `ViewClaims` and `DownloadDocument` methods.  

- **Error Handling:**  
  - The system validates file types, file sizes, and required form fields.  
  - Errors during file upload or encryption/decryption are logged and presented to the user.  
  - While all major file types are supported, error handling can be further expanded for unsupported types.  

- **Commit Count:**  
  - The project now has **12 commits**, including adding the SQL file and this README update, exceeding the minimum requirement of 10 commits.

---

## Developer Info

- **Project Developed by:** Kaeden Samsunder  
- **Student Email:** kaedensamsunder@example.com  
- **Development Environment:** Visual Studio 2022, .NET 9, SQL Server  

---

## Packages That Need to Be Installed by the User

- Microsoft.EntityFrameworkCore  
- Microsoft.EntityFrameworkCore.SqlServer  
- Microsoft.EntityFrameworkCore.Tools  
- Microsoft.AspNetCore.Session  
- Microsoft.AspNetCore.Http.Abstractions  
- Any NuGet package required for AES encryption (`System.Security.Cryptography` is included by default in .NET)  

Install using NuGet Package Manager or via CLI:  
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
```
