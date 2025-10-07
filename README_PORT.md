# Pençe Harekatı HTML Portu

Bu proje, `Pence_Harekatı.html` dosyasında yer alan canvas tabanlı prototipin Unity 2021.3+ ortamına taşınmış halidir. `Assets/Scenes/Main.unity` sahnesini açıp **Play** tuşuna bastığınızda HTML sürümündeki temel oynanış (tank kontrolü, düşman dalgaları, seviye atlama ve yükseltmeler) aynen çalışır.

## İçerik

- `Assets/Scripts/HTMLPort/Core/SpriteLibrary` sınıfı, HTML sürümündeki canvas çizimlerinden esinlenerek sprite'ları **çalışma anında** prosedürel olarak üretir. Böylece depoda ikili PNG dosyalarına ihtiyaç kalmaz.
- `Assets/Scripts/HTMLPort` altındaki klasörler HTML prototipindeki sistemlerin yeniden yazılmış C# karşılıklarını içerir:
  - `Core`: Oyun döngüsü (`GameController`), Bootstrap ve sprite yükleme altyapısı.
  - `Systems`: Oyuncu, düşman, mermi ve XP küresi davranışları.
  - `Input`: Klavye girdilerini soyutlayan `PlayerInputRouter`.
- `Assets/Resources/Configs` ileriye dönük olarak spritesheet meta dosyalarını saklamak için ayrılmıştır.

## Çalıştırma

1. `Assets/Scenes/Main.unity` sahnesini açın.
2. `GameController` nesnesi ve altındaki `Sprites` objesi tüm world öğelerini içerir; kamera ortografiktir ve piksel sadakatine göre ayarlanmıştır.
3. Play tuşuna basın. Tank WASD ile hareket eder, Shift ile takviye (dash), Space ile kalkan aktive edilir. Düşmanları vurdukça XP ve para kazanır, seviye atladığında yükseltme seçimi arayüzü açılır.

## Notlar

- Sprite'lar piksel-dostu olacak şekilde (PPU=64) üretilir ve filtreleme Point olarak ayarlanır.
- HTML sürümündeki veri akışı korunarak GameController sınıfı içinde UI ve oyun durumları yönetilir.
