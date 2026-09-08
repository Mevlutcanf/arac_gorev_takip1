import re

with open('Controllers/AdminController.cs', 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace('''        private readonly AppDbContext _db;

        public AdminController(VehicleRepository vehicleRepo, GorevFormuRepository formRepo,
            AdminUserRepository adminRepo, ArventoService arventoService, IEmailService emailService,
            IHgsService hgsService, AppDbContext db)
        {
            _vehicleRepo = vehicleRepo;
            _formRepo = formRepo;
            _adminRepo = adminRepo;
            _arventoService = arventoService;
            _emailService = emailService;
            _hgsService = hgsService;
            _db = db;
        }''', '''        private readonly AppDbContext _db;
        private readonly ISystemLogService _logService;

        public AdminController(VehicleRepository vehicleRepo, GorevFormuRepository formRepo,
            AdminUserRepository adminRepo, ArventoService arventoService, IEmailService emailService,
            IHgsService hgsService, AppDbContext db, ISystemLogService logService)
        {
            _vehicleRepo = vehicleRepo;
            _formRepo = formRepo;
            _adminRepo = adminRepo;
            _arventoService = arventoService;
            _emailService = emailService;
            _hgsService = hgsService;
            _db = db;
            _logService = logService;
        }''')

# 2. Add SystemLogService using directive if needed
if 'using AracGorevFormu.Services;' not in content:
    content = content.replace('using Microsoft.EntityFrameworkCore;', 'using Microsoft.EntityFrameworkCore;\nusing AracGorevFormu.Services;')

# 3. Remove GetClientIpAddress and LogIslemAsync
content = re.sub(r'        private string GetClientIpAddress\(\).*?await _db\.SaveChangesAsync\(\);\n        }', '', content, flags=re.DOTALL)

# 4. Replace LogIslemAsync calls
content = re.sub(r'await LogIslemAsync\((.*?)\);', r'await _logService.LogIslemWithHttpContextAsync(\1, HttpContext);', content)

# 5. Replace AracSil hard delete with soft delete
content = content.replace('''            var target = await _vehicleRepo.GetirByIdAsync(id);
            var aracSilPlaka = target?.Plaka ?? "Bilinmeyen";
            await _vehicleRepo.SilAsync(id);
            await _logService.LogIslemWithHttpContextAsync("Araç Silindi", $"{aracSilPlaka} plakalı araç silindi.", HttpContext);
            TempData["Mesaj"] = "Araç silindi.";''', '''            var target = await _vehicleRepo.GetirByIdAsync(id);
            if (target != null)
            {
                var aracSilPlaka = target.Plaka;
                target.Aktif = false;
                await _vehicleRepo.GuncelleAsync(target);
                await _logService.LogIslemWithHttpContextAsync("Araç Silindi (Pasife Alındı)", $"{aracSilPlaka} plakalı araç sistemden pasife alındı.", HttpContext);
                TempData["Mesaj"] = "Araç silindi (pasife alındı).";
            }''')

with open('Controllers/AdminController.cs', 'w', encoding='utf-8') as f:
    f.write(content)
print("Done!")
