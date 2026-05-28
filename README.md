# Rota81

`Rota81`, Türkiye haritası üzerinde şehirler arası rota takibi ve bilgi soruları ile ilerleyen, 2 oyunculu bir Unity oyun projesidir. Oyuncular seçilen rota boyunca otobüslerini ilerletir; her durakta sorulan sorulara doğru cevap veren oyuncu bir sonraki şehre geçer.

## Oyun Özeti

- 2 oyuncu adı ve renk seçimi ile başlar.
- Oyuncular hazır olduktan sonra rota seçimi ekranı gelir.
- Seçilen rota, Türkiye illeri üzerinde görselleştirilir.
- Her turda oyuncular bulundukları şehirle ilgili soruları cevaplar.
- Doğru cevap veren oyuncu rotada ilerler; yanlış cevapta oyuncu yerinde kalır.
- Rotanın son şehrine ilk ulaşan oyuncu kazanır.

## Özellikler

- Türkiye illeri üzerinde 3D rota görselleştirmesi
- Hazır bölgesel rota havuzu (Karadeniz, Akdeniz, Ege, Marmara, Doğu Anadolu, İç Anadolu)
- `Rastgele (Random)` rota seçimi
- Oyuncu bazlı isim ve renk özelleştirmesi
- Şehir bazlı soru sistemi (`questions.json`)
- Sahne geçişlerinde fade animasyonlu akış
- Tur/oyun durumunu sahneler arasında taşıyan merkezi durum yönetimi

## Kullanılan Teknolojiler

- **Unity:** `6000.3.10f1`
- **Dil:** C#
- **UI:** Unity UI + TextMeshPro
- **Render Pipeline:** Universal Render Pipeline (URP)

## Proje Akışı (Sahneler)

1. **MainMenu**
   - Ana menü, ayarlar ve oyuncu kurulum ekranları.
2. **MapScene**
   - Seçilen rotanın harita üzerinde çizimi, otobüs hareketleri, tur başlangıcı.
3. **GameScene**
   - Oyuncuların soru-cevap turu.
   - Sonuçlar kaydedilir ve tekrar `MapScene`'e dönülür.

Bu döngü rota tamamlanana kadar devam eder.

## Kurulum ve Çalıştırma

### Gereksinimler

- Unity Hub
- Unity Editor `6000.3.10f1` (önerilen sürüm)

### Adımlar

1. Bu depoyu klonlayın:
   ```bash
   git clone <repo-url>
   ```
2. Unity Hub üzerinden **Open** diyerek proje klasörünü seçin.
3. Proje açıldığında `Assets/Scenes/MainMenu.unity` sahnesini çalıştırın.
4. Oyuncu ayarlarını tamamlayıp rotayı seçerek oyunu başlatın.

## Soru Verisi

- Soru verisi dosyası: `Assets/Resources/questions.json`
- `QuestionLoader`, sahne açılışında bu dosyayı yükler.
- İller bazında soru tanımlayarak oyun içeriğini genişletebilirsiniz.

## Önemli Scriptler

- `Assets/Scripts/MainMenu/MainMenuManager.cs`  
  Ana menü panelleri ve ayar yönetimi.
- `Assets/Scripts/MainMenu/PlayerSetupManager.cs`  
  Oyuncu bilgileri, renk seçimi ve rota seçimi.
- `Assets/Scripts/MapScene/MapSceneManager.cs`  
  Harita sahnesi ana akışı, rota çizimi ve otobüs hareketleri.
- `Assets/Scripts/GameScene/GameManager.cs`  
  Soru turunun yürütülmesi ve sonuçların işlenmesi.
- `Assets/Scripts/GameState.cs`  
  Sahneler arası oyun durumu.
- `Assets/Scripts/RouteDatabase.cs`  
  Hazır rota katalogu.

## Geliştirme Notları

- Yeni rota eklemek için `RouteDatabase` içine rota adı ve şehir dizisi ekleyin.
- Yeni soru eklemek için `questions.json` dosyasındaki ilgili il alanını güncelleyin.
- Görsel geçişler `SceneFader` singleton yapısı ile yönetilir.

## Katkı

Katkı sağlamak isterseniz:

1. Fork alın.
2. Yeni bir branch açın (`feature/...`).
3. Değişikliklerinizi commit edin.
4. Pull Request gönderin.

## Lisans

Bu proje için henüz bir lisans dosyası tanımlanmadı. Uygun lisans eklemek için `LICENSE` dosyası oluşturabilirsiniz.
