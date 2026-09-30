# Cozy Board — Oyun sistemi ve hikâye taslağı

30 Eylül 2026 · İlk tasarım sürümü.

Bu belge uygulanacak yönü tanımlar. Açılış, sipariş seçimi, mektup ve kayıt altyapısı ile kısa tamir işleri ve 6 tuşlu makro pad artık projeye eklendi. Aksesuar, mouse ve final henüz uygulanmadı. Önceden paylaşılan Windows ZIP'i değişmedi. Süreler ve yeni ödüller oynanış testleriyle ayarlanacak hedeflerdir.

## İlk aşamanın uygulama durumu

- Yeni oyun Nermin'in notuyla başlar. Not kapatılınca laptopta Ece'nin ilk teklifi açılır; kabul edilmeden montaj malzemesi çıkmaz.
- İlk teslimattan sonra Ece'nin kısa tamiri, Deniz'in 6 tuşlu makro pad'i ve Mina'nın 61 tuşlu klavyesi birlikte sunulur. Aynı anda yalnızca bir aktif iş vardır.
- İlk teslimat, tamir ve makro pad için ürün ve sonuca uygun mektuplar ile atölye defterindeki gelişmeler oyuna bağlandı. Tamir/makro pad hikâye işleri tamamlandığında tekrar edilebilir kısa tekliflere dönüşür.
- Müşteri, kabul edilmiş iş kimliği, hikâye olayı ve teslimat sayısı ayrıldı. Mektuplar teslimatın müşterisini saklar.
- Kayıt sürümü 12. Eski kayıtlar mevcut siparişlerini, boya/bantlarını ve stoklarını korur; mevcut teslimatlarından sonra otomatik olarak çeşitli işlere geçerler. Nermin'in açılışını görmek için yeni oyun gerekir, kısa işleri açmak için gerekmez.
- Arayüzü kapatmayan sessiz kayıt ve geçici dosyadan atomik kayıt değişimi eklendi. Kabul veya teslimat kaydı yazılamazsa işlem geri alınır.
- Yeni üretimlere otomatik arıza atanmaz. Eski kayıttaki arızalar korunur. Ayrı tamir, takılı bir klavye ve üç komşu tuşluk testle başlar; sorun bulunmadan arızalı tuş sökülemez. Bir yedek switch tüketilir, diğer parçalar geri takılır.
- Tamir ücreti 90 Tık; tek yedek switch profil başına 8/10/12 Tık. Makro pad ücreti 100 Tık + ses/his için en fazla 30 Tık. Açılan 61'li switch ve cap setinden yalnızca altı parça tüketilir; kalan parçalar sonraki işlerde kullanılmak üzere stokta tutulur.
- Makro pad'in 3×2 gövdesi, altı tuşu, dört köşe vidası ve küçük kargo kutusu vardır. Altı işlev test kartından ayrı ayrı seçilir; yanlış veya değiştirilmiş işlev yeniden test gerektirir. Gerçek işletim sistemi makroları gönderilmez.
- Editor doğrulaması: `CozyBoard.Editor.WorkshopStoryVerify.Run`. Opt-in Windows doğrulaması: `--cozy-story-smoke --cozy-story-report <dosya>`; kullanıcı kaydı yerine ayrı geçici kayıt kullanır.
- Yeni ürün doğrulaması: `CozyBoard.Editor.WorkshopVarietyVerify.Run`; Windows runner `--cozy-variety-smoke --cozy-variety-report <dosya>`. Kayıtlar ayrı geçici dosyalara yazılır. Test Windows çıktısı `Builds/VarietyVerification/CozyBoard.exe`; inceleme görüntüleri ve raporlar `Logs/variety-*` / `Logs/windows-variety-*` altında tutulur.

## Oyuncuya verdiğimiz söz

“Küçük bir atölyede, insanların günlük hayatına iyi gelen eşyalar yap.”

Cozy Board bir üretim bandı değil. El işi hissi, kişisel tasarım ve bir müşteriyi tanıma duygusu birlikte ilerler. Yeni ürün yalnızca farklı bir model değil, farklı bir uğraş getirmeli.

- Rahat tempo: siparişlerin geri sayımı, kira baskısı, iflas veya günlük giriş zorunluluğu yok.
- Kısa ve uzun işler birlikte: oyuncu her oturumda 61 tuş takmak zorunda değil.
- Boyama ve bant korunur; renk/desen için gizli “doğru cevap” aranmaz.
- Hata geri alınabilir. Tamir, cezadan çok keşif ve çözüm hissi verir.
- Atölye büyüdükçe masayı doldurmak yerine kullanılan işe uygun ekipman çıkarılır.

## Mevcut temel ve değiştireceğimiz yerler

Mevcut kodda montaj, parça seçimi, Tık ekonomisi, boyama/bant, vida ve sökme aletleri, 61 tuş testi, paketleme, müşteri maili ve kayıt mevcut.

`WorkshopOrders` üç müşteriyi sipariş numarasına göre döndürüyor. `WorkshopGameMode` tamamlanmayı 124 takılı parçaya bağlıyor ve teslimattan sonra doğrudan yeni klavye başlatıyor. `WorkshopTesting` yeni montajlarda belirli bir switch'e arıza atıyor. Bunlar klavye için çalışıyor, fakat yeni iş türleri için genelleştirilmeli.

Önemli değişiklik: her sıfır klavyede zorunlu arıza olmayacak. Arızalar, açıkça tanımlanmış tamir işlerinde bulunacak. Yeni üretimde kontrol var; oyuncunun iyi yaptığı iş durduk yere bozuk ilan edilmiyor.

## Ana döngü

Gelen işler → birini seç → isteği ve malzemeyi incele → üret / tamir et → kontrol et → paketle → teslimat ve kısa müşteri cevabı → yeni iş veya atölye gelişimi.

Laptopta başlangıçta tek yönlendirilmiş iş, öğretimden sonra en fazla üç teklif görünür. Bunlardan biri kısa iş olur. Aynı anda masada bir aktif iş bulunur; henüz kabul edilmemiş teklifler malzeme tüketmez. Uzun işi kaydedip daha sonra devam etmek mümkündür.

Hikâye işleri kaybolmaz ve yan işler yüzünden gecikme cezası almaz. Zorunlu hikâye sırası yalnızca yeni mekaniği öğretmek için kullanılır. Sonrasında sıradaki hikâye işi ve isteğe bağlı işler beraber sunulur.

### İş türleri

| İş | Oyuncunun asıl uğraşı | İlk kapsam | Hedef süre |
| --- | --- | --- | --- |
| Özel klavye | Ses/his seçimi, montaj ve tasarım | Mevcut 61 tuşlu gövde | 10–15 dk |
| Tamir | Belirtiyi test et, sorunu bul, doğru parçayı değiştir | Tek arızalı switch | 3–5 dk |
| Makro pad | Küçük montaj, tuş işlevi seçimi ve kişiselleştirme | 6 tuş; ekran ve knob yok | 5–8 dk |
| Masa aksesuarı | Boyama ve bantla desen oluşturma | Tek bilek desteği modeli | 3–6 dk |
| Mouse | Farklı montaj ve farklı işlev testleri | Daha sonraki aşama | Testten sonra belirlenecek |

Makro pad işlevleri ilk sürümde oyun içi temsildir: oynat/duraklat, geri al, fırça büyütme gibi hazır seçenekler. İşletim sistemine gerçek makro gönderilmez.

Tamirde tüm switch seti tüketilmez; bir yedek parça tüketilir. İlk tamir, Ece'nin oyuncunun daha önce yaptığı yeni klavyesi değil, atölyeye getirdiği eski klavyesidir. Böylece önceki teslimatın kalitesi geriye dönük bozulmaz.

## İlerleme ve ekonomi

Üç ayrı ilerleme hissi yeterli; üçünü de ayrı para birimine dönüştürmeyelim:

1. Tık: malzeme ve isteğe bağlı dekor için mevcut para.
2. Ustalık: tamamlanan öğretici işler yeni iş türlerini açar; tekrar tekrar XP toplama şartı yok.
3. Mahalle ilişkileri: tamamlanmış karakter olayları yeni mektup ve ortak proje katkısı açar. Azalan arkadaşlık barı yok.

İlk taslak açılış sırası: Ece'nin klavyesi → Ece'nin eski klavyesini tamir → Deniz'in 6 tuşlu makro pad'i. Sonrasında klavye, tamir ve makro pad teklifleri birlikte gelir. Aksesuar, ilk ortak proje katkısında açılır. Mouse ancak bu döngü eğlenceli bulunduğunda eklenir.

Öğretici ekipman hikâye ödülü olarak gelir; ana hikâye para biriktirme duvarına takılmaz. Daha gelişmiş seçenekler ve dekor Tık ile alınabilir. Lehim istasyonu, hot-swap switch değiştirmek için yapay bir zorunluluk olmaz; ancak gerçekten lehim gerektiren işler eklendiğinde açılır.

Mevcut klavye fiyatları korunarak başlanır: başlangıç 320 Tık ve temel setler; set toplamı 145–195 Tık; teslimat 240 Tık ve iki müşteri isteği için en fazla 60 Tık ek ödeme. Renk/desen serbest kalır. Yeni işlerde malzeme bedeli ve emek karşılığı ayrı gösterilir; kısa tamirlerin kâr/dakikası uzun üretimi anlamsızlaştırmamalı.

Oyuncunun malzemeye parası yetmezse aktif işin gerekli malzemesine, teslimat ödemesinden mahsup edilen faizsiz tedarik seçeneği sunulur. Serbestçe harcanabilen nakit üretmez; dekor alışverişini finanse etmez. Gerekli malzeme ve ödeme kabul ekranında açıkça gösterilir.

Teslimat değerlendirmesi iki parçalıdır: çalışır durumda olması teslim şartı; belirtilen ses/his veya işlev istekleri ek memnuniyet sağlar. Görsel güzellik algoritmayla puanlanmaz. Eksik test oyuncuya gösterilir; teslim anında sürpriz başarısızlık verilmez.

## Hikâye: Bir Tuşluk Mola

Oyuncu şehirdeki yoğun hayatından sonra, bir süreliğine Nermin teyzesinin eski atölyesini devralır. Nermin hayattadır; uzun zamandır ertelediği bir yolculuğa çıkmıştır. Atölye kapanmamıştır ama uzun süredir yalnızca küçük tamirlerle ayakta kalmaktadır.

Tezgâhta eski bir defter, üç bekleyen not ve küçük bir çay fincanı vardır. Defterin kapağına Nermin şunu yazmıştır:

“Bir şeyi tamir ederken önce ne işe yaradığını değil, kimin için önemli olduğunu sor.”

Mahallenin küçük kitapçı-kafesinde kullanılmayan bir masa vardır. Ece orada çizer, Deniz yazar, Mina ise mahalle radyosu için kayıt yapar. Birlikte bu masayı herkesin oturup bir şey üretebildiği bir **Mola Köşesi**ne çevirmek isterler.

Oyuncu tek başına mahalleyi kurtaran kişi değildir. Ece kartları çizer, Deniz küçük metinler hazırlar, Mina ses köşesini kurar, Nermin eski bağlantılarını paylaşır. Oyuncu bunların kullanılacağı eşyaları yapar. Ortak proje, sıradan siparişler arasında yavaşça görünür olur.

### Karakterler

- **Nermin:** pratik, sıcak, hafif muzip bir usta. Ders anlatan bir anlatıcıdan çok, kısa notlarla yol gösterir. “Çayı klavyeden uzağa koy” gibi gündelik tavsiyeler verir.
- **Ece:** illüstratör. Sessiz klavye ister; ilerleyen işte eski ekipmanını onarırız. Oyuncuya köşe için çizdiği küçük bir kart gönderir.
- **Deniz:** yazar. Tok sesli klavye sever; metin düzenlerken kullanmak üzere makro pad ister. Önce boş sayfadan çekinir, finalde köşenin küçük hikâye defterini getirir.
- **Mina:** mahalle radyosunu kaydeder. Net tık hissini sever; hikâyenin insanları birbirine bağlayan tarafıdır. Final için sessiz bir giriş anonsu hazırlar.

Mevcut portreler ve karakter isimleri korunur. Nermin'in ilk sürümde yalnızca defter ve mektupla görünmesi yeterli; yeni portre üretimi bu aşamanın şartı değil.

### Beş bölüm

| Bölüm | Hikâye gelişimi | Oynanış karşılığı | Kalıcı küçük iz |
| --- | --- | --- | --- |
| 1 — Anahtar paspasın altında | Atölyeyi devralırız, Ece ilk işi verir | Mevcut klavye döngüsü | Ece'nin teşekkür kartı |
| 2 — Eskisi de kıymetli | Ece atamadığı eski klavyesini getirir | İlk ayrı tamir işi | Nermin'in defterine bir sayfa |
| 3 — Altı küçük kolaylık | Deniz'in yazma ritmine yardımcı oluruz | 6 tuşlu makro pad | Deniz'in kısa öyküsü |
| 4 — Bir masa, birkaç insan | Mola Köşesi fikri ortak projeye dönüşür | Bilek desteği / desen işi | Köşenin küçük fotoğrafı |
| 5 — Burada yerin var | Hazırladıklarımız bir araya gelir | Son kişisel katkı ve kontrol | Atölye adı yazılı açılış kartı |

Final pahalı eşya sayısına veya kusursuz puana bağlı değildir. Öğretilmiş işleri tamamlamak yeterlidir. Uzun bir ara sahne yerine hazırlanmış köşenin tek sıcak görüntüsü, kısa mesajlar ve yumuşak bir ses ortamı kullanılır. Finalden sonra atölye açık kalır; yeni yan işler ve serbest tasarım devam eder.

### İlk sahne metni

Tezgâhtaki not, Nermin'den:

“Anahtar paspasın altında. Fincanı bıraktım; çayını sen tazele. Ece uğrayacak, bir klavye soracaktı. Her şeyi bir günde öğrenmeye çalışma. Önce sandalyeyi kendine göre çek. Gerisi gelir.

P.S. Çekmecedeki kurabiyeler yedek parça değil. Gönül rahatlığıyla kullan.”

İlk sipariş, Ece'den:

“Merhaba! Nermin teyze artık tezgâhta senin olduğunu söyledi. Kitapçıdaki ortak masada çizim yapıyorum. Sessiz ve hafif basılan bir klavye istiyorum; yanımdakinin cümlesini bölmesin. Renkleri sana bırakıyorum. Ben de hep aynı yeşile dönüyorum zaten.”

İlk başarılı teslimat sonrası:

“Bugün yanımdaki Deniz, ben çalışmaya ne zaman başladım diye sordu. Tuşları duymamış! Bu arada kitapçıda pencerenin yanındaki boş masayı gördün mü? Oraya bir şeyler yakışır gibi geliyor. Sonra anlatırım. — Ece”

İsteklerin tamamı karşılanmadıysa aynı mektubun başlangıcı mevcut sonuçla uyumlu alternatiften seçilir; müşteri sessizlik sağlanmadığında sağlanmış gibi davranmaz. Hikâye ipucu her sonuçta verilebilir.

İlk tamir talebi:

“Bir ricam daha var. Eski klavyemin bir tuşu bazen çalışmıyor. Yenisi çok güzel ama bunu atmak istemiyorum; ilk çizim masamda hep bu vardı. Neresi olduğunu birlikte bulabilir miyiz? — Ece”

Finalde Nermin'in notu:

“Fotoğraf geldi. Masayı tanıdım ama etrafındaki neşeyi yeni görüyorum. Ben sana bir atölye bıraktığımı sanıyordum. Sen ona bir mahalle eklemişsin. Fincanı yıkama, gelince çay içeriz.”

Hikâye metinleri kısa ve geçilebilir olur. Uzun metin laptopta daha sonra okunabilir. Oyuncunun seçtiği bir tasarımın fotoğrafı/önizlemesi mümkün olduğunda müşterinin cevabına eklenir; bu ilk aşamanın bağımlılığı değildir.

## Uygulama sırası ve bitiş ölçütleri

### Aşama 1 — Sipariş ve hikâye temeli

- Sipariş numarası, müşteri kimliği, iş türü ve hikâye olayı ayrı kimlikler haline gelir.
- Laptopta teklif seçme ve aktif işi gösterme akışı kurulur; ilk öğretici iş tek teklif olabilir.
- Açılış notu, Ece'nin ilk işi ve sonuca uygun teslimat mektubu bağlanır.
- Hikâye olayı bir kez işlenir; mektubu yeniden okumak para veya ilerleme üretmez.
- Yeni oyunda yeni hikâye başlar. Eski kayıt mevcut aktif klavyeyi, boya/bandı, stokları, cüzdanı ve posta arşivini korur; açılışı zorla tekrar oynatmaz.
- Bu aşamada hâlâ yalnızca klavye oynanır. Önce geçiş ve kayıt sağlamlaştırılır.

Bitti sayılır: kabul → kaydet/yükle → test → paketle → teslim et akışı çalışır; ödül tek kez verilir; eski sürüm kaydı yüklenir; üç müşteri sessizce aynı sipariş numarasına bağlı kalmaz.

### Aşama 2 — Gerçek tamir işi

- Ece'nin eski klavyesi takılı halde gelir; tek switch arızası vardır.
- Belirti, test, sökme, yedek parça, yeniden test ve teslimat akışı kurulur.
- Mevcut yeni üretimlere zorunlu arıza atama kaldırılır; kaydedilmiş eski arızalar çözülene kadar korunur.
- Tamir sırasında yalnızca ilgili parçanın malzemesi kullanılır.

Bitti sayılır: oyuncu belirtilen arızayı bulur ve onarır; yanlış tuş sökmek geri alınabilir; mevcut boya kaybolmaz; tamir kaydı tekrar açılır; sağlıklı yeni klavye yapay arıza kazanmaz.

### Aşama 3 — 6 tuşlu makro pad

- Küçük gövde, altı switch ve altı cap; mevcut boyama ve bant stüdyosu kullanılır.
- Altı hazır işlev yuvası ve oyun içinde bunları deneyen küçük bir test yüzeyi gelir.
- Deniz'in işi ve işlevlere uygun müşteri cevabı eklenir.
- Tamamlanma, stok tüketimi ve test sayısı ürün tanımından hesaplanır; 61/124 sabitlerine dayanmaz.

Bitti sayılır: altı tuşlu ürün klavye gibi 61 sonuç istemez; kaydet/yükle ve paketleme her iki üründe çalışır; işlev seçimi testte görünür.

### Aşama 4 — Ortak proje ve aksesuar

- Bir bilek desteği modeli, desen/bant işi ve Mola Köşesi ilerlemesi eklenir.
- Tamamlanan katkılar küçük kartlarla gösterilir; masa dekoru isteğe bağlı ve sınırlıdır.
- Kısa final, final sonrası yan işler ve tekrar etmeyen müşteri metinleri eklenir.

### Aşama 5 — Oynanış testi, sonra mouse

Önce birkaç oyuncuyla şu sorular kontrol edilir: bir oturumda farklı iş seçiliyor mu, tamir gerçekten keşif hissettiriyor mu, makro pad farklı mı yoksa yalnızca kısa klavye mi, mektuplar okunuyor mu? Yeni ürün sayısından önce bu döngünün sonucu değerlendirilir.

Mouse, lehim ve baskı ekipmanı ilk pakete dahil değildir. Mouse ancak farklı bir kontrol döngüsüyle gelir: sol/sağ switch, tekerlek ve sensör testleri. Yeni model görmek tek başına yeterli değildir.

## Teknik sınırlar

- `OrderDefinition`: sabit iş kimliği, müşteri, ürün/iş türü, istekler, malzeme, ödül ve hikâye olayı. İlk aşamada basit veri tanımı; gereksiz büyük görev altyapısı yok.
- `ActiveOrderState`: kabul edilmiş iş, aşama ve işe özel durum. Parça/çizim kayıtları mevcut mekanizmayla korunur.
- `StoryState`: bölüm, işlenmiş olay kimlikleri ve açılan iş türleri. İlerleme yalnızca güvenilir teslimat olayından gelir.
- `Receipt`: o teslimatın müşteri ve metin kimliğini taşır; eski mail sonradan değişen aktif siparişten müşteri çıkarmamalı.
- `ProductDefinition`: takılacak parçalar, geçerli test noktaları ve paket boyutu; makro pad aşamasında eklenir.
- Yeni kayıt sürümünde v10 ve önceki uyum korunur. Eski `order` alanı teslimat sayacı olarak tutulabilir; hikâye bölümü sayacı yerine kullanılmaz.
- Tekrarlanan yükleme/teslim çağrısı ödülü ve hikâye olayını çoğaltmamalı. Kabul, ödül ve sonraki teklif durumu birlikte kayıt altına alınır.
- Mevcut `SaveProgress` panel/poz kapatma yan etkilerine sahip. Yeni hikâye ekranını yalnızca kaydetmek için kapatmak yerine kullanıcı arayüzünden bağımsız güvenli kayıt yolu ayrılmalı.
- Her aşamada eski boyama/bant, montaj, alet, test, mağaza ve Windows smoke kontrolleri yeniden çalıştırılır. Kullanıcı kaydı test amacıyla değiştirilmez.

## Şimdilik kapsam dışında

Serbest dolaşma, tam şehir haritası, NPC günlük programları, sesli uzun diyalog, gerçek işletim sistemi makroları, çok oyunculu mod, ürünlerin tamamını aynı anda eklemek ve dekor için yeni bir para birimi.

İlk oynanabilir hedef: kısa açılış → Ece'nin klavyesi → kişisel teşekkür → Ece'nin eski klavyesinin tamiri. Bu küçük bölüm sağlam ve sıcak hissettirdiğinde makro pad'e geçilir.
