namespace AracGorevFormu.Models
{
    public enum GorevDurumu
    {
        Beklemede = 0,      // Yeni oluşturuldu, admin onayı bekliyor
        Onaylandi = 1,      // Admin onayladı, araç kullanımda / dışarıda
        Reddedildi = 2,     // Admin reddetti
        TamamlandiDondu = 3 // Onaylandı ve araç geri teslim edildi
    }
}
