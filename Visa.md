# New Session

Exported: 6/25/2026, 1:46:25 PM

---

**User:**

Visa Expiry (4 Months), Passport Expiry Reminder (18 Months) - to allow to display report, and track the progress based on the progress list given
so fist added the oclunm in the employee master page

---

**Assistant:**

Let me first explore the existing employee-related files to understand the current structure before making changes.

### Sub-Agent: Context Gatherer

**Explanation:** Gathering context about the employee master structure to understand existing fields before adding VisaExpiryDate and PassportExpiryDate columns.

**Prompt:**

Explore the employee master related files in this project. I need to understand:
1. The Employee domain model/entity class
2. The Employee DTO classes
3. The EmployeeController
4. The Employee repository/service
5. The Angular employee module - specifically the employee master component (HTML and TS)
6. The OBMSDbContext for employee-related entities

Look in:
- d:\FWG-Malasiya\OBMSAPP-API\OBMS.WebAPI\Models\Domain\ 
- d:\FWG-Malasiya\OBMSAPP-API\OBMS.WebAPI\Models\DTO\
- d:\FWG-Malasiya\OBMSAPP-API\OBMS.WebAPI\Controllers\EmployeeController.cs
- d:\FWG-Malasiya\OBMSAPP-API\OBMS.WebAPI\Models\OBMSDbContext.cs
- d:\FWG-Malasiya\OBMSAPP-UI\angularapp\src\app\modules\employee\ (explore this folder structure)

Provide the full content of the relevant files so I can understand the current employee model structure and what columns/fields already exist.

**Response:**

Here is a complete overview of the employee master structure across the full stack:

---

## Backend (ASP.NET Core)

### 3 Domain Tables

**1. `Employee` table** (`Employee.cs`) — Core personal info:
- `EMP_ID` (PK), `EMP_CODE`, `EMP_ROLE` (Guard/Staff/None/Others)
- Personal: `EMP_NAME`, `EMP_SEX`, `EMP_DATE_OF_BIRTH`, `EMP_RACE`, `EMP_MARTIAL_STATUS`
- IC/Passport: `EMP_IC_NEW`, `EMP_IC_OLD`, `EMP_IC_COLOR`, `EMP_PASSPORT_NO`
- Address: `EMP_ADDRESS1/2`, `EMP_POST_CODE`, `EMP_TOWN`, `EMP_STATE`
- Contact: `EMP_PHONE`, `EMP_MOBILEPHONE`
- Spouse/Emergency: `EMP_SPOUSE_NAME`, `EMP_SP_IC`, `EMP_NO_CHILD`, `EMP_SP_WORK`
- Contact person address fields: `EMP_CONTACT_ADDRESS1/2`, `EMP_CONTACT_POST_CODE/TOWN/STATE/TELEPHONE`
- Branch/Transfer: `EMP_BRANCH_CODE`, `OldBranch`, `TransferDate`, `HasTransfered`
- Flags: `EMP_CITIZEN`, `EMP_CHECKLIST`, `EMP_CLIENT`, `KDNVetting`, `NewSalaryStructure`, `SalaryStructure1000_3h`
- Audit: `LASTUPDATE`, `LastUpdatedBy`

**2. `EmploymentDetails` table** (`EmploymentDetails.cs`) — Employment/salary info, keyed by `EMPPAY_CODE`:
- `EMPPAY_ID` (PK), `EMPPAY_CODE`, `EMPPAY_BRANCHCODE`
- `EMPPAY_JOB_TITLE`, `EMPPAY_CATEGORY`
- Dates: `EMPPAY_DATE_JOINED`, `EMPPAY_DATE_CONFIRM`, `EMPPAY_DATE_PROMOTION`, `EMPPAY_DATE_RESIGNED`
- Pay: `EMPPAY_BASIC_RATE`, `SALARYLAB`, `ATTENDANCEALLOWANCE`, `NewStructureATTENDANCEALLOWANCE`, `SpecialAllowance`, `KPI`
- Allowance config: `AttendanceAllowanceWorkingDays`, `AttendanceAllowanceFollowCalendar`

**3. `EmployeeSalaryDetails` table** (`EmployeeSalaryDetails.cs`) — Financial/statutory info, keyed by `EMPFL_CODE`:
- `EMPFL_ID` (PK), `EMPFL_CODE`, `EMPFL_BRANCHCODE`
- Bank: `EMPFL_BANK`, `EMPFL_BK_ACCNO`, `EMPFL_2ndBank`, `EMPFL_2ndBK_ACCNO`, `SplitSalaryPayment`
- Statutory: `EMPFL_TAX_NO`, `EMPFL_EPFNO`, `EMPFL_EPF8Pa`, `EMPFL_SOSCO_NO`
- Flags: `EPFDETECT`, `SOCSODETECT`, `TMPGUARD`, `DETECTBYND55`, `INCOMETAXDETECT`
- Payment: `PAYMODE`, `PAYMODE2`, `EMP_SP_TEL_NO`

**4. `EmployeeHistory` table** — Mirror of all 3 tables combined for audit trail

---

### DTOs
- `EmployeeRequestDto` — Combined flat DTO used for Save/Update (merges all 3 tables + extra fields like `EMP_SP_TEL_NO`, `PAYMODE2`, `SplitSalaryPayment`, `EMPFL_2ndBank/ACCNO`, `KPI`)
- `EmployeeDto` — Flat response DTO with core Employee + some EmploymentDetails/SalaryDetails fields
- `EmployeeWithEmploymentDTO` — Used in list queries: wraps `Employee` entity + `EMPPAY_DATE_JOINED` + `EMPPAY_CATEGORY`
- `EmployeeHistoryDto` — Full flat DTO mirroring the history table
- `EmployeeTransferDto` — `EMP_ID`, `FROM_BRANCH_ID`, `TO_BRANCH_ID`, `TRANSFER_DATE`

---

### EmployeeController endpoints
- `GET /EmployeeNoByBranchID?branchId=`
- `GET /Employees?name=` — Returns employees (filtered by user's allowed branches) + branch list
- `GET /EmployeesByBranchId?branchId=`
- `GET /EmployeeById?employeeId=`
- `GET /GetEmployeeMaster?userId=`
- `GET /GetClientsFromBranchId?branchId=&empType=`
- `GET /GetEmployeeNo`
- `GET /GetSalarySlabList?employeeType=&nonStructure=`
- `POST /SaveAndUpdateEmployee` — Saves/updates all 3 tables atomically
- `POST /UpdateEmployeeTransfer`
- `GET /CheckEmployeeInfo?from=&data=`
- `GET /GetAllEmployeesWithHistory?branch=`

---

### OBMSDbContext — Employee-related DbSets
```csharp
DbSet<Employee> Employees
DbSet<EmploymentDetails> EmploymentDetails
DbSet<EmployeeSalaryDetails> EmployeeSalaryDetails
DbSet<EmployeeHistory> EmployeeHistories
DbSet<EmployeeItemIssue> EmployeeItemIssues
```

---

## Frontend (Angular)

**Location:** `src/app/modules/master/employee-master/`

### List Component (`employee-master.component`)
- Mat-table with columns: EMP_CODE, EMP_BRANCH_CODE, EMP_ROLE, EMP_NAME (+ joined date), EMP_SEX, EMP_IC_NEW, EMP_PASSPORT_NO, EMP_TOWN, action
- Branch filter dropdown, text search filter
- Role-based access: `Employee Master` + `Employee Master Staff Admin` permissions
- Filters out `Staff` role employees for non-staff-admin users

### Form Component (`new-employee.component`) — used for both Add and Edit
Form sections with these fields:
1. **Basic**: Type (Guard/Staff/None/Others), Employee No, Branch, Client, Name, Sex, DOB, Age
2. **IC Details**: Nationality (Malaysian/Foreigner/PR), Country, IC New, Passport, Race
3. **Address**: Address1/2, Postal Code, City, State, Phone, Mobile, Marital Status, Work Experience, Education
4. **Spouse/Emergency**: Name, IC, Working, Tel No, No of Children, Emergency address fields
5. **Employment Details**: Job Title, Category, Join Date, Resigned Date
6. **Salary Details**: Salary Group (slab), SalaryStructure1000_3h (4HRS/3HRS/2HRS/2.5HRS/2HRS KPI), Basic Rate, Allowance, Attendance Working Days, Follow Calendar, Special Allowance, KPI, Payment Mode, Bank/Account, Split Salary, 2nd Bank, Temp Employee, Income Tax, SOCSO, EPF, EPF No, Tax No, SOCSO No, Detect by ND55
7. **Checklist** (bitmask stored in `EMP_CHECKLIST`): Application Form, Copy of IC, Bank Details, Photos, Passport, Visa, Appointment Letter, Confirmation Letter, KDN Vetting, CSG/TNG

The Angular employee module is actually inside `src/app/modules/master/employee-master/`, not a standalone `employee` module.

Let me explore the Domain and DTO folders, then batch read all relevant files in parallel.

Now let me find the Angular employee module (it appears to be inside `master` or `administration`) and also read the OBMSDbContext and EmployeeSalaryDetails domain model:

I now have all the key files. Let me provide the complete summary to the parent agent.

Now I have a clear picture of the structure. I need to add `VisaExpiryDate` and `PassportExpiryDate` to the `Employee` domain model, and wire them through the DTO, controller, and Angular form. Let me do all the changes now.

Now update the controller's `SaveAndUpdateEmployee` to map the new fields:

Now update the Angular form — add `VisaExpiryDate` and `PassportExpiryDate` to the `frm` FormGroup in the TS file:

Now also add the date formatting in `onSubmit()` for the new date fields:

Now add the fields to the HTML form. They belong in the IC Details section since passport/visa are related. I'll add them after the Passport field (which already only shows for foreigners) and after Race — since both visa and passport expiry apply mainly to foreigners:

Now generate the EF migration SQL. Let me check if there's a migration project or if they run raw SQL:

Migrations folder is empty — this project uses raw SQL scripts. Let me create the migration SQL script:

Now let me verify the diagnostics on the changed files to make sure there are no issues:

All clean. Here's a summary of every change made:

**Backend (API)**

1. `Employee.cs` — Added `VisaExpiryDate` and `PassportExpiryDate` as `DateTime?` properties to the `Employee` domain model.

2. `EmployeeRequestDto.cs` — Added the same two nullable date fields so they flow through the save/update endpoint.

3. `EmployeeController.cs` — Mapped both new fields in `SaveAndUpdateEmployee` so they persist to the DB when saving.

**Frontend (Angular)**

4. `new-employee.component.ts` — Added `VisaExpiryDate` and `PassportExpiryDate` form controls to the `frm` FormGroup, plus date formatting in `onSubmit()`.

5. `new-employee.component.html` — Added two datepicker fields (Passport Expiry Date and Visa Expiry Date) in the IC Details section, shown only when the employee is a **Foreigner** (`*ngIf="isForeigner"`).

**Database**

6. `Employee_VisaPassportExpiry_Migration.sql` — Idempotent SQL script to `ALTER TABLE Employee` adding both columns. Run this against your database before deploying the API.

The fields are gated behind `isForeigner` since visa/passport expiry only applies to non-Malaysian employees. If you also want them visible for all employees (e.g. for PR holders), just remove the `*ngIf="isForeigner"` condition.

---

**User:**

ippo popup vekkalam ya

---

**Assistant:**

இப்போ விஸா மற்றும் பாஸ்போர்ட் காலாவதி தேதிகளை எச்சரிக்கை popup-ஆக காட்ட வேண்டுமா? அதாவது:

1. **விஸா காலாவதி** - 4 மாதங்களுக்கு முன் எச்சரிக்கை
2. **பாஸ்போர்ட் காலாவதி** - 18 மாதங்களுக்கு முன் எச்சரிக்கை

எங்கெல்லாம் popup காட்ட வேண்டும்:
- Dashboard-ல் login பண்ணும் போதா?
- Employee master list page-ல் நுழையும் போதா?
- அல்லது தினமும் ஒரு scheduled reminder-ஆ?

எந்த இடத்தில் reminder காட்ட வேண்டும் என்று சொல்லுங்கள், அதற்கேற்ப:
- Backend-ல் API endpoint உருவாக்குவேன் (upcoming expiries list)
- Frontend-ல் popup component உருவாக்குவேன்
- Mat-dialog வழியாக employee list-ஐ காட்டுவேன்

எப்படி வேண்டும்?

---

**User:**

cystal report msri panitalam eppdi na naa master click panna reprot load akumari like "Report/Payment Due List" ippdi

---

**Assistant:**

Crystal Report style-ல் செய்யணும் — அதாவது "Report/Payment Due List" மாதிரி **Employee Master** menu click பண்ணும்போதே ஒரு popup வரணும், அதுல:

- Visa Expiry (4 months) உள்ள employees
- Passport Expiry (18 months) உள்ள employees

list-ஆ காட்டணும். 

முதல்ல existing report structure எப்படி இருக்குன்னு பார்க்கிறேன்:

இப்போது pattern தெளிவா புரிஞ்சது. முழு structure பார்த்தோம். இப்போ இந்த pattern-ஐ follow பண்ணி:

1. **UatReports/Master/** - ASPX + .cs + .rpt (Crystal Report)
2. **Angular** - Report component (iframe based)
3. **Employee Master** click → popup with iframe showing report

Let me check the routing and module files:

Perfect! இப்போ எனக்கு தெளிவா pattern புரிஞ்சது. இப்போ நான் **employee visa/passport expiry reminder report** create பண்றேன். Complete structure:

## Summary of what I'll create:

### 1. Backend (SQL View/Query)
- New view or query that gets employees with upcoming visa/passport expiry

### 2. UatReports (Crystal Report)
- `Master/EmployeeExpiryReminder.aspx` + `.cs` + `.rpt`

### 3. Angular Component
- `report/master/employee-expiry-reminder/` component
- Route: `/master/report/employee-expiry-reminder`

Let me create all files:

Now the Crystal Report ASPX + code-behind:

---

**User:**

contine

---

**Assistant:**

சரி, இப்போ Crystal Report ASPX pages மற்றும் Angular component-ஐ create பண்றேன்:

Now the Angular component files:

Now register the component in the routing module:

Now find and update the shared module to declare the component:

Now add the import and declaration in shared.module.ts:

Perfect! இப்போ ஒரு **README file** உருவாக்கலாம் — Crystal Report (`.rpt`) file எப்படி create பண்ணுவது என்று:

எல்லாம் clean! இதோ complete summary:

---

## Create பண்ணிய Files:

### 📁 Database
| File | Purpose |
|------|---------|
| `VWEmployeeExpiryReminder.sql` | SQL View — Active foreigners with expiring visa (4 months) or passport (18 months) |

### 📁 UatReports/Master (Crystal Report)
| File | Purpose |
|------|---------|
| `EmployeeExpiryReminder.aspx` | Crystal Report viewer page |
| `EmployeeExpiryReminder.aspx.cs` | Code-behind — DB connection + parameter binding |
| `EmployeeExpiryReminderReport_INSTRUCTIONS.md` | Manual `.rpt` file create பண்ண guide |

### 📁 Angular Component
| File | Purpose |
|------|---------|
| `employee-expiry-reminder.component.ts` | Branch dropdown + Expiry Type filter + iframe load |
| `employee-expiry-reminder.component.html` | UI — Filter form + iframe |
| `employee-expiry-reminder.component.css` | Styles |

### Updated Files
- `shared.module.ts` — Component declared
- `admin-layout-routing.module.ts` — Route added: `/master/report/employee-expiry-reminder`

---

## இனி செய்ய வேண்டியது:
1. **SQL run பண்ணு** — `VWEmployeeExpiryReminder.sql` + `Employee_VisaPassportExpiry_Migration.sql`
2. **Crystal Reports Designer-ல்** `EmployeeExpiryReminderReport.rpt` create பண்ணு (Instructions file பார்க்கவும்)
3. Sidebar menu-ல் link add பண்ண சொல்லு — நான் add பண்றேன்

---

**User:**

Master ithu la list add panu side bar vena

---

**Assistant:**

சரி, sidebar-ல் Master menu-க்குள்ள link add பண்றேன். முதல்ல sidebar component எங்க இருக்குன்னு பார்க்கலாம்:

Perfect! Master section-ல் Reports பகுதில்ல add பண்றேன்:

Done! ஒரே ஒரு line மட்டும் add பண்ணினேன்:

```html
<li class="nav-item" [routerLink]="['master/report/employee-expiry-reminder']">
  <a class="nav-link">Visa & Passport Expiry</a>
</li>
```

**Master → Reports** section-ல் "Employee List" க்கு கீழே, "KKDN" க்கு மேலே இப்போ தெரியும்:

```
Master
  ├── Branch Master
  ├── Client Master
  ├── Employee Master
  ├── ...
  └── Reports
        ├── Branch Report
        ├── Client Report
        ├── Employee List
        ├── Visa & Passport Expiry   ← NEW
        └── KKDN
```

Navigate பண்ணும்போது `/master/report/employee-expiry-reminder` route-க்கு போகும், Crystal Report iframe-ல் load ஆகும்.

---

**User:**

inga illa click panna laod pannum paramiter la vena

---

**Assistant:**

புரிஞ்சது! "Search button click பண்ணாமலே, page load ஆகும்போதே உடனே report load ஆகணும்" — அதாவது parameter இல்லாம directly report show ஆகணும். 

EmployeeList pattern மாதிரி — `ngOnInit`-லேயே URL build பண்ணி iframe load பண்றோம். Let me update the component:

Now update to auto-load on init (like EmployeeList pattern):

Perfect! இப்போ:

## இப்போ எப்படி Work பண்ணும்:

1. **Sidebar-ல் "Visa & Passport Expiry" click**
2. Page load ஆகும்
3. User access check → Branch list load
4. **உடனே automatically report load** ஆகும் (default parameters: Branch=All, ExpiryType=All)
5. User வேணும்னா Branch அல்லது Expiry Type மாத்தி **Search** button click பண்ணலாம்

## இதுக்கு பிறகு செய்ய வேண்டியவை:

### ✅ Already Done:
- ✅ Employee table-ல் `VisaExpiryDate`, `PassportExpiryDate` columns add
- ✅ SQL View create (`VWEmployeeExpiryReminder`)
- ✅ ASPX pages create (UatReports)
- ✅ Angular component create + routing
- ✅ Sidebar menu link add
- ✅ Auto-load on page open

### 📋 Todo:
1. **Run SQL scripts**:
   - `Employee_VisaPassportExpiry_Migration.sql`
   - `VWEmployeeExpiryReminder.sql`

2. **Create Crystal Report file** (`EmployeeExpiryReminderReport.rpt`):
   - Follow `EmployeeExpiryReminderReport_INSTRUCTIONS.md`
   - Save `.rpt` file in `UatReports/Master/` folder

3. **Test**:
   - Navigate: Master → Reports → Visa & Passport Expiry
   - Report should auto-load with all employees expiring soon

Complete! 🎉

---

**User:**

EmployeeExpiryReminderReport.rpt eppdi pannum

---

**Assistant:**

`.rpt` file — Crystal Reports Designer-ல் manually create பண்ணணும். ஆனா existing `.rpt` file copy பண்ணி modify பண்றது easy. இதோ step-by-step screenshots மாதிரி guide:

`.rpt` files are binary — text editor-ல் edit பண்ண முடியாது. Crystal Reports Designer-ல் மட்டும் open/create பண்ண முடியும். இதோ exact steps:

---

## Step-by-Step: EmployeeExpiryReminderReport.rpt Create பண்றது

### முதல்ல — Shortcut Method (Existing .rpt Copy)

**EmployeeReport.rpt-ஐ base-ஆ use பண்ணலாம்:**

Copy ஆச்சு. இப்போ Crystal Reports Designer-ல் modify பண்ணணும். இதோ exact steps:

---

## 🖥️ Crystal Reports Designer-ல் செய்ய வேண்டியது

### **Step 1: File Open பண்ணு**
```
Visual Studio → Open File
→ d:\FWG-Malasiya\UatReports\Master\EmployeeExpiryReminderReport.rpt
```

---

### **Step 2: Database மாத்து (முக்கியம்!)**

```
Top Menu → Database → Database Expert
```

இப்போ இப்படி தெரியும்:
```
Selected Tables:
  ├── Employee        ← இதை REMOVE பண்ணு
  └── SalaryAdvance   ← இதை REMOVE பண்ணு
```

**Remove பண்ண:** Selected table-ஐ select பண்ணி `<` button click

**Add பண்ண:**
```
Create New Connection
  → OLE DB (ADO)
    → Microsoft OLE DB Provider for SQL Server
      → Server: DESKTOP-94UNK78\SQLEXPRESS
      → Database: obmsuat
      → Next → Finish

Views folder expand பண்ணு
  → VWEmployeeExpiryReminder ← இதை > button click பண்ணி add பண்ணு
```

Click **OK**

---

### **Step 3: Existing Fields எல்லாம் Delete பண்ணு**

Report body-ல் இருக்க எல்லா fields-உம் select பண்ணி Delete

---

### **Step 4: Parameters Add பண்ணு**

Left panel → **Field Explorer** → **Parameter Fields** → Right click → **New**

இந்த 10 parameters create பண்ணு (எல்லாம் `String` type):

| Parameter Name | Type |
|---|---|
| CompanyName | String |
| CompanyAddress1 | String |
| CompanyAddress2 | String |
| CompanyAddress3 | String |
| CompanyAddress4 | String |
| CompanyRegistration | String |
| CompanyPhone | String |
| Branch | String |
| ExpiryType | String |
| LoginID | String |

---

### **Step 5: Page Header Section — Company Details**

**Insert → Text Object** — இவைகளை Page Header-ல் வையுங்கள்:

```
{?CompanyName}          ← Bold, Center, Font Size 14
{?CompanyAddress1}
{?CompanyAddress2}
{?CompanyAddress3} {?CompanyAddress4}
{?CompanyPhone}
```

**Title Text Object:**
```
"Employee Visa & Passport Expiry Reminder"   ← Bold, Center, Font Size 12
```

**Print Date:**
```
Insert → Special Field → Print Date
```

---

### **Step 6: Column Headers (Details Header Section)**

**Insert → Text Object** — இந்த column headers வையுங்கள்:

```
| No | Emp Code | Name | Branch | Passport No | Visa Expiry | Days | Passport Expiry | Days | Type |
```

---

### **Step 7: Detail Section — Fields Add பண்ணு**

**Field Explorer → Database Fields → VWEmployeeExpiryReminder** expand பண்ணு

இந்த fields-ஐ drag பண்ணி Detail section-ல் drop பண்ணு:

```
EMP_CODE
EMP_NAME
EMP_BRANCH_CODE
EMP_PASSPORT_NO
VisaExpiryDate          ← Format: dd/MM/yyyy
PassportExpiryDate      ← Format: dd/MM/yyyy
DaysToVisaExpiry
DaysToPassportExpiry
ExpiryType
```

---

### **Step 8: Conditional Color Formatting (Red/Orange)**

`DaysToVisaExpiry` field → Right click → **Format Field** → **Font** tab → Color → Formula button (x+2):

```crystal
If IsNull({VWEmployeeExpiryReminder.DaysToVisaExpiry}) Then crBlack
Else If {VWEmployeeExpiryReminder.DaysToVisaExpiry} <= 30 Then crRed
Else If {VWEmployeeExpiryReminder.DaysToVisaExpiry} <= 60 Then RGB(255, 140, 0)
Else crBlack
```

Same formula `DaysToPassportExpiry`-க்கும் apply பண்ணு.

---

### **Step 9: Record Selection Formula**

```
Report Menu → Selection Formulas → Record
```

```crystal
(
  {?Branch} = "All" 
  OR {VWEmployeeExpiryReminder.EMP_BRANCH_CODE} = {?Branch}
)
AND
(
  {?ExpiryType} = "All"
  OR
  (
    {?ExpiryType} = "Visa" 
    AND NOT IsNull({VWEmployeeExpiryReminder.DaysToVisaExpiry})
    AND {VWEmployeeExpiryReminder.DaysToVisaExpiry} <= 120
  )
  OR
  (
    {?ExpiryType} = "Passport"
    AND NOT IsNull({VWEmployeeExpiryReminder.DaysToPassportExpiry})
    AND {VWEmployeeExpiryReminder.DaysToPassportExpiry} <= 540
  )
)
```

---

### **Step 10: Sort Order**

```
Report Menu → Record Sort Expert
→ Add: EMP_BRANCH_CODE (Ascending)
→ Add: DaysToVisaExpiry (Ascending)
```

---

### **Step 11: Report Footer — Total Count**

**Insert → Summary** → Count of `EMP_CODE`

Text: `"Total Employees: "`

---

### **Step 12: Save**

```
File → Save
→ d:\FWG-Malasiya\UatReports\Master\EmployeeExpiryReminderReport.rpt
```

---

### **Step 13: Test பண்ணு**

```
Crystal Reports → Preview tab click
→ Parameters prompt வரும்:
   Branch: All
   ExpiryType: All
   LoginID: admin
   CompanyName: FREIGHTWATCH...
   (மற்றவை Web.config-லிருந்து automatically வரும்)
```

---

## ✅ Quick Checklist

```
☐ Step 2: VWEmployeeExpiryReminder view connected
☐ Step 4: 10 parameters created
☐ Step 5: Company header in Page Header
☐ Step 6: Column headers added
☐ Step 7: All 9 fields in Detail section
☐ Step 8: Red/Orange color for expiry days
☐ Step 9: Record selection formula added
☐ Step 10: Sort by Branch, Days
☐ Step 12: Saved as EmployeeExpiryReminderReport.rpt
```

---

## 🔥 Tip: Existing Report Reference-ஆ Use பண்ண

**EmployeeList.aspx.cs** pattern already follow பண்றோம். `EmployeeReport.rpt`-ஐ open பண்ணி அதோட layout-ஐ பார்த்து same style-ல் create பண்ணலாம் — அது already Visual Studio-ல் இருக்கு, open பண்ணி reference-ஆ பாருங்க!

---

**User:**

top la MAster iruku la atha lcick pannum side bar open aganum intha rpeort open akanum

---

**Assistant:**

Report working பண்றது! 🎉 இப்போ top navbar-ல் **"Master"** click பண்ணும்போது sidebar open ஆகி report page navigate ஆகணும். Navbar component பார்க்கிறேன்:

இப்போ புரிஞ்சது! `toggleSubmenu('master')` click பண்ணும்போது `dashboard`-க்கு navigate பண்றது — ஆனா நீங்க சொல்றது: **Master click → sidebar open + report page navigate ஆகணும்**.

`header.component.ts`-ல் `toggleSubmenu` function மாத்தணும்:

Now sidebar auto-open ஆகணும் — `activeMenu = 'master'` set ஆகும்போது sidebar-ல் master section expand ஆகுது, ஆனா `/master/...` route-ல் navigate ஆகும்போது sidebar component-ல் already `url.startsWith('/master')` check இருக்கு. ✅

Verify பண்றோம்:

Clean! இப்போ flow இப்படி வேலை பண்ணும்:

---

## Flow:

```
Top Navbar "Master" click
       ↓
toggleSubmenu('master')
       ↓
_dataService.setMenuName('master')   ← sidebar 'master' section open ஆகும்
       ↓
navigate('/master/report/employee-expiry-reminder')
       ↓
Sidebar: url.startsWith('/master') → activeMenu = 'master' ✅
Report: Auto-load ஆகும் (branches load ஆன உடனே) ✅
```

---

## உங்க screenshot-ல் பார்த்தது இப்போ:

| Before | After |
|--------|-------|
| Master click → Dashboard | Master click → **Visa & Passport Expiry Report** |
| Sidebar manually open | Sidebar **automatically Master section open** |

மற்ற menu buttons (Finance, Payroll etc.) dashboard-க்கே போகும் — அவை default behavior-ஐ maintain பண்றோம். Master மட்டும் directly report-க்கு போகும்.

---

**User:**

All (Visa + Passport)
Expiry Type
All Branches
Branch
remove this paramiters andserach button also i wnat only lcick this Visa Expiry (4 Months) details and Passport Expiry Reminder (18 Months only not at all

---

**Assistant:**

புரிஞ்சது! Parameters, Search button எல்லாம் remove பண்ணி — page load ஆகும்போதே **2 separate tabs/buttons** மாதிரி:

- **"Visa Expiry (4 Months)"** click → Visa report load
- **"Passport Expiry (18 Months)"** click → Passport report load

Both files update பண்றேன்:

Now update the TS — remove form, branch list, debounce. Keep only `loadReport(type)`:

Clean! இப்போ page இப்படி தெரியும்:

```
┌─────────────────────────────────────────────────────────┐
│  Report / Employee Visa & Passport Expiry Reminder       │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  [● Visa Expiry (4 Months)]  [  Passport Expiry (18 Months)  ]  │
│                                                          │
│  ┌──────────────── Crystal Report iframe ─────────────┐ │
│  │  Visa expiry employees list...                     │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

- Page open ஆகும்போதே **Visa Expiry** button active ஆகி report load ஆகும்
- **Passport Expiry (18 Months)** click பண்ணினா அந்த report load ஆகும்
- Active button highlighted (primary color), inactive button gray
- Parameters, Search button — எதுவும் இல்ல! Clean UI ✅

---

**User:**

Visa button clecik panna Passport  colunm show panna koodathu

---

**Assistant:**

இது Crystal Report `.rpt` file-ல் மாத்தணும் — Angular-ல் control பண்ண முடியாது. `ExpiryType` parameter-ஐ report-ல் column suppress formula-ஆ use பண்ணணும்.

Crystal Reports Designer-ல் இப்படி பண்ணு:

---

## Crystal Reports Designer — Column Suppress Formula

### **PassportExpiryDate column — Suppress பண்ண:**

**PassportExpiryDate field** → Right click → **Format Field** → **Common tab** → **Suppress** → Formula button **(x+2)**:

```crystal
{?ExpiryType} = "Visa"
```

**Same formula — இந்த fields-க்கும் apply பண்ணு:**

| Field | Suppress Formula |
|-------|-----------------|
| PassportExpiryDate | `{?ExpiryType} = "Visa"` |
| DaysToPassportExpiry | `{?ExpiryType} = "Visa"` |
| PassportExpiryDate **Column Header** (Text Object) | `{?ExpiryType} = "Visa"` |
| DaysToPassportExpiry **Column Header** (Text Object) | `{?ExpiryType} = "Visa"` |

---

### **VisaExpiryDate column — Suppress பண்ண:**

| Field | Suppress Formula |
|-------|-----------------|
| VisaExpiryDate | `{?ExpiryType} = "Passport"` |
| DaysToVisaExpiry | `{?ExpiryType} = "Passport"` |
| VisaExpiryDate **Column Header** (Text Object) | `{?ExpiryType} = "Passport"` |
| DaysToVisaExpiry **Column Header** (Text Object) | `{?ExpiryType} = "Passport"` |

---

### **ExpiryType column — எப்போதும் hide:**

`ExpiryType` field → Format Field → Common → **Suppress** → tick ✅ (formula இல்லாம directly)

---

### **இதோ Step-by-step:**

```
1. EmployeeExpiryReminderReport.rpt open பண்ணு

2. PassportExpiryDate field → Right click
   → Format Field
   → Common tab
   → Suppress checkbox-க்கு அடுத்த (x+2) button click
   → Formula Editor-ல் type பண்ணு:
      {?ExpiryType} = "Visa"
   → Save & Close

3. DaysToPassportExpiry field → Same steps
   → {?ExpiryType} = "Visa"

4. "Passport Expiry" Column Header Text Object → Same steps  
   → {?ExpiryType} = "Visa"

5. "Days" (Passport side) Column Header → Same steps
   → {?ExpiryType} = "Visa"

6. VisaExpiryDate field → Right click → Format Field
   → Suppress formula:
      {?ExpiryType} = "Passport"

7. DaysToVisaExpiry → Same
   → {?ExpiryType} = "Passport"

8. "Visa Expiry" Column Header → Same
   → {?ExpiryType} = "Passport"

9. "Days" (Visa side) Column Header → Same
   → {?ExpiryType} = "Passport"

10. File → Save
```

---

### **Preview-ல் Test பண்ணு:**

```
ExpiryType = "Visa"    → Visa columns மட்டும் தெரியும்
                          Passport columns hide ஆகும் ✅

ExpiryType = "Passport" → Passport columns மட்டும் தெரியும்
                           Visa columns hide ஆகும் ✅
```

---

### **Important Note:**
Column suppress பண்ணும்போது **column width** adjust ஆகாது — blank space வரும். அதை fix பண்ண:

Suppress formula-க்கு பதிலா **Can Grow / Can Shrink** option use பண்ணலாம், அல்லது columns-ஐ report-ல் இடம் மாத்தி layout adjust பண்ணலாம்.

---
