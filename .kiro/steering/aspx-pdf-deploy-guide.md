# ASPX → PDF Report : எப்படி Work ஆகுது & எப்படி Deploy பண்றது

> **இந்த project-ல உள்ள example:** `UatReports-new/UatReports/Inventory/StockLedgerReport.aspx`
> Future-ல புது report போட்டாலும், existing report fix பண்ணினாலும் இதே pattern follow பண்ணு.

---

## 1. Overall Architecture — ஒரு பார்வையில்

```
Angular UI (browser)
    │
    │  iframe src = "http://<ReportServer>/Inventory/StockLedgerReport.aspx?LoginID=...&StartDate=...&WarehouseID=..."
    ▼
ASP.NET WebForms Report Server  (UatReports-new/UatReports)
    │
    │  StockLedgerReport.aspx  →  reads query string params
    │  →  runs SQL Stored Procedure  (sp_InventoryStockLedger)
    │  →  builds PDF using iTextSharp
    │  →  streams PDF bytes to browser
    ▼
Browser shows PDF inline (or downloads Excel if ?format=excel)
```

---

## 2. இந்த ASPX File-ல என்ன இருக்கு

### 2-A. Query String Parameters (Angular UI இதை அனுப்புது)

| Parameter         | என்ன                          | Example              |
|-------------------|-------------------------------|----------------------|
| `LoginID`         | Login பண்ணின user name        | `superadmin`         |
| `StartDate`       | From date                     | `2026-01-01`         |
| `EndDate`         | To date                       | `2026-09-30`         |
| `WarehouseID`     | Warehouse table ID (int)      | `1`                  |
| `ItemID`          | Item table ID, 0 = All        | `0`                  |
| `TransactionType` | ALL / OPENING / STOCK_IN etc  | `ALL`                |
| `format`          | pdf (default) or excel        | `pdf`                |

### 2-B. ASPX-ல என்ன நடக்குது — Step by Step

```
Page_Load()
  ├── 1. Query string படிக்கும்
  ├── 2. Validate பண்ணும் (date empty-ஆ? warehouse 0-ஆ?)
  ├── 3. RunSP() — SQL Stored Procedure execute பண்ணும்
  │       └── sp_InventoryStockLedger (@WarehouseID, @ItemID, @TransactionType, @FromDate, @ToDate)
  ├── 4. Data வந்தா:
  │       ├── format=excel  → BuildExcel() → HTML table as .xls download
  │       └── format=pdf    → BuildPdf()   → iTextSharp PDF → browser-ல inline காட்டும்
  └── 5. Error வந்தா → SendError() → HTML error page காட்டும்
                     → LogErr()   → ReportParamLog.txt-ல write பண்ணும்
```

### 2-C. Connection String எங்கிருந்து வருது

`Web.config` → `<appSettings>` section:

```xml
<add key="Server"   value=".\SQLEXPRESS" />
<add key="Database" value="OBMSUATNEW"   />
<add key="UserID"   value="sa"           />
<add key="Password" value="server@123"   />
```

ASPX-ல `ConnStr()` function இதை படிக்கும்:
```csharp
string.Format("Server={0};Database={1};User Id={2};Password={3};",
    AppSettings("Server"), AppSettings("Database"),
    AppSettings("UserID"),  AppSettings("Password"));
```

---

## 3. புது Report ASPX Create பண்ணும் போது — Checklist

> **ஒவ்வொரு report-க்கும் இந்த 4 பொருட்கள் வேணும்:**

### Step 1 — SQL Stored Procedure எழுது

`Database/` folder-ல புது `.sql` file போடு (எ.கா: `18.MyNewReport_Migration.sql`):

```sql
CREATE PROCEDURE sp_MyNewReport
    @FromDate   DATE,
    @ToDate     DATE,
    @BranchCode VARCHAR(50) = ''
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ... FROM ... WHERE ...
END
```

### Step 2 — ASPX File உருவாக்கு

`UatReports-new/UatReports/<Module>/MyNewReport.aspx` create பண்ணு.

**File structure இப்படி இருக்கணும்:**

```aspx
<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="System.IO" %>
<script runat="server">

    protected void Page_Load(object sender, EventArgs e)
    {
        // 1. Read query string
        string loginID   = (Request.QueryString["LoginID"]   ?? "").Trim();
        string fromStr   = (Request.QueryString["StartDate"] ?? "").Trim();
        string toStr     = (Request.QueryString["EndDate"]   ?? "").Trim();

        // 2. Validate
        DateTime from, to;
        if (!DateTime.TryParse(fromStr, out from) || !DateTime.TryParse(toStr, out to))
        { SendError("Invalid date range."); return; }

        // 3. Run SP
        DataTable dt;
        try { dt = RunSP(from, to); }
        catch (Exception ex) { LogErr("RunSP", ex.Message); SendError("DB error."); return; }

        if (dt == null || dt.Rows.Count == 0)
        { SendError("No data found."); return; }

        // 4. Build PDF
        byte[] pdf;
        try { pdf = BuildPdf(loginID, dt); }
        catch (Exception ex) { LogErr("BuildPdf", ex.Message); SendError("PDF error."); return; }

        // 5. Stream to browser
        Response.Clear();
        Response.ContentType = "application/pdf";
        Response.AddHeader("Content-Disposition", "inline; filename=MyReport.pdf");
        Response.AddHeader("Content-Length", pdf.Length.ToString());
        Response.AddHeader("Access-Control-Allow-Origin", "*");
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.End();
    }

    private byte[] BuildPdf(string loginID, DataTable dt)
    {
        using (var ms = new MemoryStream())
        {
            var doc    = new Document(PageSize.A4, 25f, 25f, 30f, 30f);
            var writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();
            // ... add content ...
            doc.Close();
            return ms.ToArray();
        }
    }

    private DataTable RunSP(DateTime from, DateTime to)
    {
        var dt = new DataTable();
        using (var conn = new SqlConnection(ConnStr()))
        using (var cmd  = new SqlCommand("sp_MyNewReport", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FromDate", from.Date);
            cmd.Parameters.AddWithValue("@ToDate",   to.Date);
            conn.Open();
            new SqlDataAdapter(cmd).Fill(dt);
        }
        return dt;
    }

    private static string ConnStr()
    {
        return string.Format("Server={0};Database={1};User Id={2};Password={3};",
            ConfigurationManager.AppSettings["Server"],
            ConfigurationManager.AppSettings["Database"],
            ConfigurationManager.AppSettings["UserID"],
            ConfigurationManager.AppSettings["Password"]);
    }

    private void SendError(string msg)
    {
        Response.Clear(); Response.ContentType = "text/html";
        Response.Write("<html><body style='font-family:Arial;padding:30px'>" +
            "<h3 style='color:#c62828'>Report Error</h3><p>" +
            Server.HtmlEncode(msg) + "</p></body></html>");
        Response.End();
    }

    private void LogErr(string src, string msg)
    {
        try {
            string path = Server.MapPath("~/ReportParamLog.txt");
            System.IO.File.AppendAllText(path,
                string.Format("[{0}] ERROR in {1}: {2}\n",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), src, msg));
        } catch { }
    }

</script>
```

### Step 3 — Angular UI-ல Route & Component இணை

**`admin-layout-routing.module.ts`** — route add:
```typescript
{ path: 'report/my-new-report', component: MyNewReportComponent, canActivate: [AuthGuard] }
```

**Component `.ts`-ல:**
```typescript
url: string = environment.baseReportUrl + 'ModuleName/MyNewReport.aspx?';

onSubmit(): void {
  let params = 'LoginID=' + this.currentUser;
  params += '&StartDate=' + this.returnDate(raw.StartDate);
  params += '&EndDate='   + this.returnDate(raw.EndDate);
  this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + params);
}
```

**Component `.html`-ல:**
```html
<iframe [src]="urlSafe" style="width:100%; height:800px; border:none;"></iframe>
```

### Step 4 — Sidebar-ல Menu Add பண்ணு

`sidebar.component.html`-ல:
```html
<li class="nav-item" routerLink="/report/my-new-report">
  <a class="nav-link">My New Report</a>
</li>
```

---

## 4. UAT-கு Deploy பண்ணும் போது — Exact Steps

> **இந்த 4 components ஒவ்வொன்னும் தனித்தனியா deploy பண்றோம்.**

---

### 4-A. DATABASE — SQL Migration Run பண்ணு

1. Server-ல SQL Server Management Studio திற
2. Database: `OBMSUATNEW` select பண்ணு
3. `Database/` folder-ல **புது migration files** (run பண்ணாதவை) திற
4. Execute பண்ணு
5. Error வந்தா fix பண்ணி மறுபடியும் run பண்ணு

```
⚠️  RULE: Migration files ஒரே ஒரு முறை மட்டும் run பண்ணு.
          Already run பண்ணினதை மறுபடியும் run பண்ணாதே.
          ஒவ்வொரு file-லயும் IF NOT EXISTS check இருக்கு — safe.
```

---

### 4-B. API — .NET 7 Publish & Copy

**Step 1: Publish பண்ணு (VS-ல)**
```
Right-click OBMS.WebAPI project
→ Publish
→ FolderProfile (publish to: OBMSAPP-API/OBMS.WebAPI/publish/)
→ Publish button click
```

**Or command line:**
```powershell
cd d:\FWG-Malasiya\OBMSAPP-API\OBMS.WebAPI
dotnet publish -c Release -o ./publish
```

**Step 2: `appsettings.json` UAT connection string set பண்ணு**

UAT-க்கு deploy பண்றதுக்கு முன்னாடி `publish/appsettings.json` check பண்ணு:
```json
"ConnectionStrings": {
  "obms": "Server=INSTANCE-OBMS-N\\SQLEXPRESS;Database=OBMSUATNEW;User ID=obmsuser;Password=obms@9999!;..."
}
```

```
⚠️  RULE: publish/ folder-ல appsettings.json-ஐ மட்டும் edit பண்ணு.
          Source code-ல உள்ள appsettings.json-ஐ UAT credentials-ஆ மாத்தாதே.
```

**Step 3: Server-ல Copy பண்ணு**
```
publish/ folder-ல உள்ள எல்லா files-ம்
→ UAT Server: 124.217.236.37
→ IIS Site folder (Port 8227 / 64499)
→ Paste (existing files overwrite பண்ணு)
```

**Step 4: IIS Restart பண்ணு**
```
IIS Manager → Application Pools → OBMS App Pool → Recycle
(or) iisreset command
```

---

### 4-C. REPORTS — ASPX Files Copy பண்ணு

**Step 1: புது/modified ASPX files identify பண்ணு**

`UatReports-new/UatReports/` folder-ல என்ன மாத்தினோம்:
- புது `.aspx` file போட்டோமா?
- Existing `.aspx` fix பண்ணினோமா?

**Step 2: அந்த files மட்டும் server-ல copy பண்ணு**
```
Local:  d:\FWG-Malasiya\UatReports-new\UatReports\Inventory\StockLedgerReport.aspx
        ↓
Server: C:\inetpub\wwwroot\UatReports\Inventory\StockLedgerReport.aspx
        (or wherever IIS Port 8229 points to)
```

**Step 3: புது report-க்கு DLL தேவையா check பண்ணு**

| Library           | DLL File                  | எப்போ தேவை                        |
|-------------------|---------------------------|-----------------------------------|
| iTextSharp        | `itextsharp.dll`          | PDF generate பண்ணும் போது         |
| BouncyCastle      | `BouncyCastle.Crypto.dll` | iTextSharp dependency             |
| Crystal Reports   | `CrystalDecisions.*.dll`  | .rpt file use பண்ணும் போது        |

```
⚠️  CRITICAL: iTextSharp use பண்ணும் புது ASPX போட்டா,
              bin/ folder-ல itextsharp.dll + BouncyCastle.Crypto.dll
              இரண்டும் இருக்கா confirm பண்ணு.
              இல்லன்னா "Unable to generate PDF" error வரும்.
```

**Step 4: Web.config-ல DB connection check பண்ணு**
```xml
<!-- UAT-க்கு இப்படி இருக்கணும் -->
<add key="Server"   value=".\SQLEXPRESS" />
<add key="Database" value="OBMSUATNEW"   />
<add key="UserID"   value="sa"           />
<add key="Password" value="server@123"   />
```

**Step 5: IIS Application Pool Recycle பண்ணு**
```
Reports site App Pool → Recycle
```

---

### 4-D. UI — Angular Build & Copy

**Step 1: `environment.prod.ts` correct URLs இருக்கா check பண்ணு**

```typescript
// UAT-க்கு:
baseUrl:       'http://124.217.236.37:8227/api/',
baseReportUrl: 'http://124.217.236.37:8229/'

// FWG Live-க்கு:
baseUrl:       'http://124.217.236.37:64499/api/',
baseReportUrl: 'http://124.217.236.37:8008/'
```

**Step 2: Build பண்ணு**
```powershell
cd d:\FWG-Malasiya\OBMSAPP-UI\angularapp
ng build --configuration production
```
Output: `dist/angularapp/` folder-ல files வரும்

**Step 3: Server-ல Copy பண்ணு**
```
dist/angularapp/ உள்ள எல்லாமே
→ UAT Server IIS site folder (Port 8644 or wherever Angular is hosted)
→ Existing files overwrite
```

**Step 4: Browser cache clear பண்ணு**
```
Ctrl + Shift + R (hard refresh) — always first test-ல பண்ணு
```

---

## 5. Error வந்தா — Debug எப்படி பண்றது

### 5-A. Report "Unable to generate PDF" வந்தா

```
1. UatReports/ReportParamLog.txt படி
   → Last few lines-ல error message இருக்கும்

2. Common errors:
   ┌─────────────────────────────────────────────────────────────┐
   │ ERROR                          │ FIX                        │
   ├────────────────────────────────┼────────────────────────────┤
   │ sp_XXX not found               │ SQL migration run பண்ணு   │
   │ BouncyCastle not found         │ bin/ கு DLL copy பண்ணு    │
   │ Invalid date range             │ UI-ல date format check     │
   │ Please select a warehouse      │ WarehouseID=0 வருது        │
   │ No data found                  │ DB-ல data இல்லை / SP bug  │
   └─────────────────────────────────────────────────────────────┘
```

### 5-B. API Error வந்தா

```
1. Server-ல IIS logs பார்
2. appsettings.json-ல connection string correct-ஆ இருக்கா check
3. App Pool running-ஆ இருக்கா check
```

### 5-C. Angular UI Load ஆகலன்னா

```
1. environment.prod.ts-ல URLs correct-ஆ இருக்கா check
2. ng build பண்ணி மறுபடியும் deploy பண்ணு
3. Browser console-ல (F12) error பார்
```

---

## 6. Deploy Checklist — Quick Reference

Copy பண்ணி use பண்ணு:

```
UAT DEPLOY CHECKLIST
====================
[ ] 1. DATABASE  : புது migration SQL files OBMSUATNEW-ல run பண்ணினேன்
[ ] 2. API       : dotnet publish பண்ணி publish/ folder ready
[ ] 3. API       : publish/appsettings.json-ல UAT connection string set பண்ணினேன்
[ ] 4. API       : Server-ல IIS folder-க்கு copy பண்ணினேன்
[ ] 5. API       : App Pool recycle பண்ணினேன்
[ ] 6. REPORTS   : Modified ASPX files server-ல copy பண்ணினேன்
[ ] 7. REPORTS   : bin/ folder-ல required DLLs இருக்கு (itextsharp, BouncyCastle)
[ ] 8. REPORTS   : Web.config DB connection UAT-ஆ இருக்கு
[ ] 9. REPORTS   : App Pool recycle பண்ணினேன்
[ ] 10. UI       : environment.prod.ts-ல UAT URLs set பண்ணினேன்
[ ] 11. UI       : ng build --configuration production run பண்ணினேன்
[ ] 12. UI       : dist/ files server-ல copy பண்ணினேன்
[ ] 13. TEST     : Browser-ல hard refresh (Ctrl+Shift+R) பண்ணி test பண்ணினேன்
```

---

## 7. Server Port Reference

| Component      | UAT Port | FWG Live Port | Database          |
|----------------|----------|---------------|-------------------|
| API            | 8227     | 64499         | OBMSUATNEW / FWGServerDB |
| Reports (ASPX) | 8229     | 8008          | Same DB as API    |
| Angular UI     | 8644     | 80 / 443      | —                 |

---

## 8. இந்த Project-ல Files எங்கே இருக்கு

```
d:\FWG-Malasiya\
├── OBMSAPP-API\OBMS.WebAPI\          ← .NET 7 API (C#)
│   ├── Controllers\                  ← API endpoints
│   ├── Repositories\                 ← DB queries
│   ├── appsettings.json              ← Local DB connection
│   └── publish\                      ← Build output (server-ல copy பண்றது இதை)
│
├── OBMSAPP-UI\angularapp\src\        ← Angular UI
│   ├── app\modules\                  ← Feature modules
│   ├── app\components\sidebar\       ← Sidebar menu
│   └── environments\
│       ├── environment.ts            ← Local dev URLs
│       └── environment.prod.ts       ← UAT / Live URLs  ← இதை deploy-க்கு முன் check பண்ணு
│
├── UatReports-new\UatReports\        ← ASP.NET Reports (ASPX)
│   ├── Inventory\                    ← Inventory reports
│   ├── Finance\                      ← Finance reports
│   ├── Payroll\                      ← Payroll reports
│   ├── bin\                          ← DLLs (itextsharp.dll இங்கே)
│   ├── Web.config                    ← DB connection + CompanyName etc
│   └── ReportParamLog.txt            ← Error logs (debug-க்கு இதை பார்)
│
└── Database\                         ← SQL Migration files
    ├── 01.xxx_Migration.sql          ← Numbered order-ல run பண்ணு
    ├── 02.xxx_Migration.sql
    └── ...
```


---

## 9. புது Server / Fresh Machine Setup — Libraries எப்படி Install பண்றது

> **எப்போ தேவை:** புது server வாங்கினோம், fresh Windows machine-ல reports host பண்றோம்,
> or existing server-ல "DLL not found" error வருது — இந்த section பார்.

---

### 9-A. இந்த Project-ல Use ஆகும் Libraries — Full List

| Library                        | Version    | எதுக்கு                              | DLL File                        |
|-------------------------------|------------|---------------------------------------|---------------------------------|
| **iTextSharp**                | 5.5.13.3   | PDF generate பண்ண                    | `itextsharp.dll`                |
| **BouncyCastle.Crypto**       | 1.8.9.0    | iTextSharp-ன் dependency (crypto)    | `BouncyCastle.Crypto.dll`       |
| **Crystal Reports Engine**    | 13.0.4003  | .rpt file reports                     | `CrystalDecisions.*.dll` (பல)  |
| **Crystal Reports Web**       | 13.0.4003  | Crystal viewer in browser             | `CrystalDecisions.Web.dll`      |
| **log4net**                   | 2.0.12     | Logging                               | (Crystal Reports use பண்ணும்)  |
| **Microsoft CodeDom**         | 2.0.1.0    | ASP.NET compile support               | `Microsoft.CodeDom.*.dll`       |

---

### 9-B. Method 1 — Local `bin/` Folder-இல் இருந்து Copy பண்ணு (EASIEST ✅)

> **இதுவே best method.** புது server-ல NuGet install தேவையில்லை.
> எல்லா DLLs-உம் already `UatReports-new/UatReports/bin/` folder-ல இருக்கு.

```
Step 1: இந்த folder முழுக்க copy பண்ணு:
        d:\FWG-Malasiya\UatReports-new\UatReports\bin\

Step 2: புது server-ல Reports IIS folder-ல paste பண்ணு:
        C:\inetpub\wwwroot\UatReports\bin\   (or your IIS path)

Step 3: ASPX files copy பண்ணு:
        UatReports-new\UatReports\Inventory\*.aspx   → Server Inventory\
        UatReports-new\UatReports\Finance\*.aspx     → Server Finance\
        UatReports-new\UatReports\Payroll\*.aspx     → Server Payroll\
        UatReports-new\UatReports\images\            → Server images\
        UatReports-new\UatReports\Web.config         → Server root\

Step 4: Web.config-ல DB connection update பண்ணு (புது server DB details)

Step 5: IIS-ல Application Pool → .NET 4.8 / Integrated Pipeline set பண்ணு
```

---

### 9-C. Method 2 — NuGet-இல் இருந்து Download பண்ணு

> **எப்போ use பண்ணு:** bin/ folder இல்லன்னா, or fresh install வேணும்னா.

**iTextSharp install (PDF library):**
```powershell
# NuGet CLI use பண்ணு (nuget.exe workspace root-ல இருக்கு)
d:\FWG-Malasiya\nuget.exe install iTextSharp -Version 5.5.13.3 -OutputDirectory packages

# இதுல இரண்டு DLLs வரும்:
# packages\iTextSharp.5.5.13.3\lib\net40\itextsharp.dll
# packages\iTextSharp.5.5.13.3\lib\net40\BouncyCastle.Crypto.dll
```

**அல்லது NuGet Package Manager Console (Visual Studio)-ல:**
```
Install-Package iTextSharp -Version 5.5.13.3
```

**Crystal Reports install:**
```
Install-Package CrystalReports.Engine -Version 13.0.4003
Install-Package CrystalReports.Web    -Version 13.0.4003
```

---

### 9-D. DLL Files — Bin Folder-ல இருக்கணும் (Minimum Required)

**iTextSharp PDF reports-க்கு (MUST HAVE இரண்டும்):**
```
bin/
├── itextsharp.dll              ← version: 5.5.13.3
└── BouncyCastle.Crypto.dll     ← version: 1.8.9.0
```

**Crystal Reports .rpt reports-க்கு:**
```
bin/
├── CrystalDecisions.CrystalReports.Engine.dll
├── CrystalDecisions.Web.dll
├── CrystalDecisions.Shared.dll
├── CrystalDecisions.ReportSource.dll
├── CrystalDecisions.ReportAppServer.ClientDoc.dll
├── CrystalDecisions.ReportAppServer.Controllers.dll
├── CrystalDecisions.ReportAppServer.DataDefModel.dll
├── CrystalDecisions.ReportAppServer.ReportDefModel.dll
└── ... (மற்ற CrystalDecisions.*.dll files)
```

---

### 9-E. IIS Setup — புது Server-ல Configure பண்ணு

```
Step 1: IIS Install பண்ணு (Windows Server)
        Server Manager → Add Roles → Web Server (IIS)
        ✅ ASP.NET 4.8 feature enable பண்ணு

Step 2: .NET Framework 4.8 install பண்ணு
        https://dotnet.microsoft.com/download/dotnet-framework/net48

Step 3: IIS-ல New Website / Application create பண்ணு
        Site Name  : UatReports
        Port       : 8229  (UAT) / 8008 (Live)
        Path       : C:\inetpub\wwwroot\UatReports\

Step 4: Application Pool settings:
        .NET CLR Version   : v4.0
        Managed Pipeline   : Integrated
        Identity           : ApplicationPoolIdentity (or specific user)

Step 5: Folder permissions set பண்ணு
        C:\inetpub\wwwroot\UatReports\  →  IIS_IUSRS = Read & Execute
        C:\inetpub\wwwroot\UatReports\  →  NETWORK SERVICE = Read & Execute
        (ReportParamLog.txt write-க்கு Write permission வேணும்)
```

---

### 9-F. Verify பண்ணு — Setup சரியா இருக்கா

```
Test 1: Browser-ல directly ASPX URL திற:
        http://server-ip:8229/Inventory/StockLedgerReport.aspx?LoginID=test&StartDate=2026-01-01&EndDate=2026-09-30&WarehouseID=1

Test 2: Error வந்தா ReportParamLog.txt பார்:
        C:\inetpub\wwwroot\UatReports\ReportParamLog.txt

Test 3: Common setup errors:
        ┌────────────────────────────────────────────┬──────────────────────────────────────────┐
        │ Error                                      │ Fix                                      │
        ├────────────────────────────────────────────┼──────────────────────────────────────────┤
        │ Could not load itextsharp.dll              │ bin/ ல itextsharp.dll copy பண்ணு        │
        │ Could not load BouncyCastle Version=1.8.9  │ bin/ ல BouncyCastle.Crypto.dll copy     │
        │ HTTP 500 — compilation error               │ .NET 4.8 install பண்ணு                  │
        │ HTTP 403 — access denied                   │ IIS_IUSRS folder permission கொடு        │
        │ sp_XXX not found                           │ SQL migration script run பண்ணு          │
        │ Cannot connect to SQL Server               │ Web.config connection string check       │
        └────────────────────────────────────────────┴──────────────────────────────────────────┘
```

---

### 9-G. Quick Copy Script — புது Server Setup

```powershell
# இந்த script-ஐ local machine-ல run பண்ணு
# புது server-ல UatReports folder ready பண்ணும்

$src = "d:\FWG-Malasiya\UatReports-new\UatReports"
$dst = "\\SERVER-IP\c$\inetpub\wwwroot\UatReports"   # ← server IP மாத்து

# bin/ முழுக்க copy (all DLLs)
Copy-Item "$src\bin"     -Destination "$dst\bin"     -Recurse -Force

# ASPX report files copy
Copy-Item "$src\Inventory" -Destination "$dst\Inventory" -Recurse -Force
Copy-Item "$src\Finance"   -Destination "$dst\Finance"   -Recurse -Force
Copy-Item "$src\Payroll"   -Destination "$dst\Payroll"   -Recurse -Force
Copy-Item "$src\Accounting"-Destination "$dst\Accounting"-Recurse -Force
Copy-Item "$src\Common"    -Destination "$dst\Common"    -Recurse -Force
Copy-Item "$src\Master"    -Destination "$dst\Master"    -Recurse -Force
Copy-Item "$src\images"    -Destination "$dst\images"    -Recurse -Force
Copy-Item "$src\crystalreportviewers13" -Destination "$dst\crystalreportviewers13" -Recurse -Force

# Config file copy (Web.config-ல DB details update பண்றதை மறக்காதே!)
Copy-Item "$src\Web.config" -Destination "$dst\Web.config" -Force

Write-Output "Copy done. Now update Web.config DB connection on server!"
```

> ⚠️ **Copy பண்ணிட்டு Web.config-ல server DB credentials update பண்றதை மறக்காதே!**
