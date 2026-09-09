import re

file_path = r'c:\Users\tatlicipc\Desktop\AracGorevFormu\Controllers\AdminController.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix the broken 
 strings
content = content.replace(']
        public', ']\n        public')

# Roles to update
replacements = {
    'Index': 'Ana Yönetici,AracIslemleri,MakineIslemleri,FormOnaylama,AyarlarYonetimi',
    'Araclar': 'Ana Yönetici,AracIslemleri',
    'AracEkle': 'Ana Yönetici,AracIslemleri',
    'AracDuzenle': 'Ana Yönetici,AracIslemleri',
    'AracSil': 'Ana Yönetici,AracIslemleri',
    'HgsBorc': 'Ana Yönetici,AracIslemleri',
    'HgsEkle': 'Ana Yönetici,AracIslemleri',
    'HgsSil': 'Ana Yönetici,AracIslemleri',
    'Bakimlar': 'Ana Yönetici,AracIslemleri',
    'BakimEkle': 'Ana Yönetici,AracIslemleri',
    'BakimSil': 'Ana Yönetici,AracIslemleri',
    'GorevFormlari': 'Ana Yönetici,AracIslemleri,FormOnaylama',
    'ResmiTutanak': 'Ana Yönetici,AracIslemleri,FormOnaylama',
    'TumGorevFormlari': 'Ana Yönetici,AracIslemleri,FormOnaylama',
    'FormOnayla': 'Ana Yönetici,FormOnaylama',
    'FormReddet': 'Ana Yönetici,FormOnaylama',
    'AracIade': 'Ana Yönetici,FormOnaylama,AracIslemleri',
    'Ayarlar': 'Ana Yönetici,AyarlarYonetimi',
    'AyarlarYoneticiEkle': 'Ana Yönetici,AyarlarYonetimi',
    'AyarlarYoneticiSil': 'Ana Yönetici,AyarlarYonetimi',
    'AyarlarSmtp': 'Ana Yönetici,AyarlarYonetimi',
    'AyarlarArvento': 'Ana Yönetici,AyarlarYonetimi',
    'MailModulu': 'Ana Yönetici,AyarlarYonetimi',
    'MailGonder': 'Ana Yönetici,AyarlarYonetimi',
    'TaslakKaydet': 'Ana Yönetici,AyarlarYonetimi',
    'TaslakSil': 'Ana Yönetici,AyarlarYonetimi',
    'AyarlarVeritabaniTest': 'Ana Yönetici,AyarlarYonetimi'
}

for func, roles in replacements.items():
    pattern = r'\[Authorize\([^\]]*\)\](\s+)public\s+async\s+Task<IActionResult>\s+' + func + r'\b'
    replacement = r'[Authorize(Roles = "' + roles + r'")]\1public async Task<IActionResult> ' + func
    content = re.sub(pattern, replacement, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Done")
