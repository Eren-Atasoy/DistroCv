# Gereksinimler Dokümanı - DistroCV v2.0

## Giriş

DistroCV v2.0, yapay zeka destekli bir kariyer asistanı platformudur. Sistem, adayın dijital ikizini oluşturarak sadece yüksek eşleşme skoruna sahip pozisyonlara kişiselleştirilmiş başvurular yapar. Platform, akıllı eşleştirme, dinamik özgeçmiş optimizasyonu, hibrit dağıtım stratejisi ve mülakat hazırlık özellikleri sunar.

## Sözlük (Glossary)

- **System**: DistroCV platformunun tüm bileşenlerini kapsayan yazılım sistemi
- **Gemini_Engine**: Google Gemini 1.5 Pro & Flash AI motoru
- **Match_Score**: İş ilanı ile aday profili arasındaki uyum yüzdesi (0-100)
- **Candidate**: Platformu kullanan iş arayan kullanıcı
- **Job_Posting**: Taranmış ve sisteme alınmış iş ilanı
- **Digital_Twin**: Adayın özgeçmiş, beceri ve tercihlerinden oluşan dijital profili
- **Tailored_Resume**: Belirli bir iş ilanı için özelleştirilmiş özgeçmiş
- **Browser_Extension**: Playwright tabanlı tarayıcı otomasyon bileşeni
- **Application_Queue**: Onay bekleyen başvuru listesi
- **Interview_Coach**: Mülakat hazırlık ve simülasyon modülü
- **Throttle_Manager**: Anti-bot koruma ve hız sınırlama yöneticisi
- **User_Database**: PostgreSQL veritabanı (pgvector ile)
- **AWS_Infrastructure**: Bulut altyapı bileşenleri (Lambda, S3, Cognito)
- **Verified_Company_Database**: Doğrulanmış şirket bilgileri ve iletişim detayları veritabanı
- **Sector_Filter**: Sektörel filtreleme modülü
- **Geographic_Filter**: Coğrafi konum bazlı filtreleme modülü

## Gereksinimler

### Gereksinim 1: Kullanıcı Profili ve Dijital İkiz Oluşturma

**Kullanıcı Hikayesi:** Aday olarak, özgeçmişimi ve tercihlerimi sisteme yükleyerek dijital ikizimin oluşturulmasını istiyorum, böylece platform benim adıma akıllı kararlar verebilsin.

#### Kabul Kriterleri

1. WHEN Candidate özgeçmiş dosyası yüklediğinde (PDF, DOCX, TXT), THEN System SHALL dosyayı parse ederek yapılandırılmış veri çıkarmalıdır
2. WHEN özgeçmiş parse edildiğinde, THEN Gemini_Engine SHALL adayın becerilerini, deneyimlerini ve kariyer hedeflerini analiz ederek Digital_Twin oluşturmalıdır
3. WHEN Digital_Twin oluşturulduğunda, THEN System SHALL veriyi User_Database'e pgvector formatında kaydetmelidir
4. WHEN Candidate profil tercihlerini güncellediğinde, THEN System SHALL Digital_Twin'i gerçek zamanlı olarak güncellemeli ve değişiklikleri loglamalıdır
5. THE System SHALL adayın hassas verilerini (şifreler, oturum bilgileri) asla sunucuda saklamamalıdır

### Gereksinim 2: İş İlanı Tarama ve Toplama

**Kullanıcı Hikayesi:** Aday olarak, sistemin benim için sürekli olarak uygun iş ilanlarını taramasını istiyorum, böylece manuel arama yapmama gerek kalmasın.

#### Kabul Kriterleri

1. THE System SHALL günlük olarak LinkedIn, Indeed ve diğer iş platformlarından iş ilanlarını otomatik taramalıdır
2. WHEN iş ilanı tarandığında, THEN System SHALL ilan metnini, şirket bilgilerini ve gereksinimlerini yapılandırılmış formatta çıkarmalıdır
3. WHEN ilan verisi çıkarıldığında, THEN System SHALL her Job_Posting için benzersiz bir tanımlayıcı oluşturmalı ve User_Database'e kaydetmelidir
4. WHEN aynı ilan tekrar tarandığında, THEN System SHALL duplikasyonu tespit etmeli ve yeni kayıt oluşturmamalıdır
5. THE System SHALL günlük en az 1000 ilan tarama kapasitesine sahip olmalıdır

### Gereksinim 3: Akıllı Eşleştirme ve Semantik Analiz

**Kullanıcı Hikayesi:** Aday olarak, sadece gerçekten uygun olduğum pozisyonları görmek istiyorum, böylece zamanımı boşa harcamayayım.

#### Kabul Kriterleri

1. WHEN yeni Job_Posting sisteme eklendiğinde, THEN Gemini_Engine SHALL Digital_Twin ile Job_Posting arasında semantik analiz yapmalıdır
2. WHEN semantik analiz tamamlandığında, THEN Gemini_Engine SHALL 0-100 arası Match_Score hesaplamalıdır
3. WHEN Match_Score hesaplandığında, THEN System SHALL skoru Job_Posting ile ilişkilendirerek kaydetmelidir
4. IF Match_Score < 80 ise, THEN System SHALL ilanı Application_Queue'ya eklememeli ve red gerekçesini loglamalıdır
5. WHEN Match_Score >= 80 olduğunda, THEN System SHALL ilanı Application_Queue'ya eklemeli ve Candidate'e bildirim göndermelidir
6. THE Gemini_Engine SHALL eşleştirme kararının gerekçesini açıklayıcı metin olarak üretmelidir

### Gereksinim 4: Dinamik Özgeçmiş Optimizasyonu

**Kullanıcı Hikayesi:** Aday olarak, her iş ilanı için özelleştirilmiş özgeçmiş oluşturulmasını istiyorum, böylece başvurularım daha etkili olsun.

#### Kabul Kriterleri

1. WHEN Candidate bir başvuruyu onayladığında, THEN Gemini_Engine SHALL Job_Posting gereksinimlerine göre Tailored_Resume oluşturmalıdır
2. WHEN Tailored_Resume oluşturulurken, THEN Gemini_Engine SHALL adayın gerçek deneyimlerini değiştirmeden anahtar kelimeleri optimize etmelidir
3. WHEN özgeçmiş optimize edildiğinde, THEN System SHALL orijinal ve optimize edilmiş versiyonları yan yana göstermelidir
4. THE Gemini_Engine SHALL her Tailored_Resume için kişiselleştirilmiş kapak mektubu (cover letter) üretmelidir
5. WHEN kapak mektubu oluşturulurken, THEN Gemini_Engine SHALL şirketin web sitesi, blog yazıları veya kültürel değerlerini analiz ederek kişiselleştirme yapmalıdır
6. THE System SHALL Tailored_Resume'yi PDF formatında dışa aktarma imkanı sunmalıdır

### Gereksinim 5: Hibrit Başvuru Dağıtım Stratejisi

**Kullanıcı Hikayesi:** Aday olarak, başvurularımın hem e-posta hem de LinkedIn üzerinden güvenli bir şekilde gönderilmesini istiyorum.

#### Kabul Kriterleri

1. WHEN Candidate başvuruyu onayladığında ve e-posta yöntemi seçildiğinde, THEN System SHALL Candidate'in Gmail API'sı üzerinden kişiselleştirilmiş e-posta göndermelidir
2. WHEN e-posta gönderilirken, THEN Gemini_Engine SHALL İK sorumlusuna hitap eden profesyonel bir mesaj oluşturmalıdır
3. WHEN Candidate başvuruyu onayladığında ve LinkedIn yöntemi seçildiğinde, THEN Browser_Extension SHALL Candidate'in yerel tarayıcısında "Easy Apply" işlemini gerçekleştirmelidir
4. WHEN Browser_Extension çalışırken, THEN System SHALL rastgele fare hareketleri ve insan benzeri yazma hızları kullanmalıdır
5. THE Browser_Extension SHALL tüm hassas işlemleri Candidate'in kendi IP adresi üzerinden yapmalıdır
6. WHEN başvuru tamamlandığında, THEN System SHALL başvuru durumunu "Gönderildi" olarak işaretlemeli ve zaman damgası kaydetmelidir

### Gereksinim 6: Anti-Bot Koruma ve Hız Sınırlama

**Kullanıcı Hikayesi:** Aday olarak, hesabımın LinkedIn tarafından engellenmemesini istiyorum, bu yüzden sistem güvenli limitler içinde çalışmalı.

#### Kabul Kriterleri

1. THE Throttle_Manager SHALL günlük maksimum 20 LinkedIn bağlantı isteği sınırı uygulamalıdır
2. THE Throttle_Manager SHALL günlük maksimum 50-80 mesaj gönderimi sınırı uygulamalıdır
3. WHEN Browser_Extension işlem yaparken, THEN Throttle_Manager SHALL işlemler arasına 2-8 dakika arası rastgele bekleme süreleri eklemelidir
4. WHEN günlük limit aşıldığında, THEN System SHALL yeni başvuruları kuyruğa almalı ve ertesi güne ertelemelidir
5. THE System SHALL ağır veri işleme görevlerini AWS_Infrastructure'da, hassas başvuru işlemlerini ise yerel Browser_Extension'da çalıştırmalıdır

### Gereksinim 7: Tinder Tarzı Kullanıcı Arayüzü

**Kullanıcı Hikayesi:** Aday olarak, eşleşen iş ilanlarını hızlı ve eğlenceli bir şekilde gözden geçirmek istiyorum.

#### Kabul Kriterleri

1. WHEN Candidate uygulamayı açtığında, THEN System SHALL Application_Queue'daki Match_Score >= 80 olan ilanları göstermelidir
2. WHEN ilan gösterilirken, THEN System SHALL iş tanımını, şirket bilgilerini, Match_Score'u ve eşleştirme gerekçesini sunmalıdır
3. WHEN Candidate sağa kaydırdığında, THEN System SHALL başvuruyu onaylamalı ve dağıtım sürecini başlatmalıdır
4. WHEN Candidate sola kaydırdığında, THEN System SHALL ilanı reddedilmiş olarak işaretlemeli ve Application_Queue'dan çıkarmalıdır
5. THE System SHALL her ilan için görsel olarak çekici ve mobil uyumlu kart tasarımı sunmalıdır

### Gereksinim 8: Mülakat Hazırlık ve Koçluk

**Kullanıcı Hikayesi:** Aday olarak, başvurduğum pozisyonlar için mülakat hazırlığı yapmak istiyorum.

#### Kabul Kriterleri

1. WHEN başvuru gönderildiğinde, THEN Interview_Coach SHALL o pozisyon ve şirket için 10 olası mülakat sorusu üretmelidir
2. WHEN mülakat soruları üretildiğinde, THEN System SHALL soruları Candidate'e sunmalı ve simülasyon başlatma seçeneği vermelidir
3. WHEN Candidate simülasyon başlattığında, THEN Interview_Coach SHALL sesli veya metin tabanlı mülakat simülasyonu yapmalıdır
4. WHEN Candidate bir soruyu cevapladığında, THEN Interview_Coach SHALL cevabı analiz etmeli ve STAR tekniği bazlı geri bildirim vermelidir
5. THE Interview_Coach SHALL Candidate'in zayıf kaldığı alanlarda iyileştirme önerileri sunmalıdır

### Gereksinim 9: Veri Gizliliği ve Güvenlik (KVKK & GDPR)

**Kullanıcı Hikayesi:** Aday olarak, kişisel verilerimin güvenli bir şekilde saklanmasını ve kontrolümde olmasını istiyorum.

#### Kabul Kriterleri

1. THE System SHALL tüm hassas verileri AES-256 şifreleme ile saklamalıdır
2. THE System SHALL Candidate'in şifrelerini ve oturum bilgilerini asla sunucuda saklamamalıdır
3. WHEN Candidate hesabını sildiğinde, THEN System SHALL tüm kişisel verileri 30 gün içinde kalıcı olarak silmelidir
4. THE System SHALL kullanılmayan verileri 30 gün sonra otomatik olarak anonimleştirmeli veya silmelidir
5. WHEN Candidate veri indirme talebinde bulunduğunda, THEN System SHALL tüm kişisel verileri yapılandırılmış formatta (JSON/PDF) sunmalıdır
6. THE System SHALL KVKK ve GDPR gerekliliklerine tam uyum sağlamalıdır

### Gereksinim 10: Gerçek Zamanlı Dashboard ve Analitik

**Kullanıcı Hikayesi:** Aday olarak, başvuru sürecimin ilerleyişini ve istatistiklerimi görmek istiyorum.

#### Kabul Kriterleri

1. THE System SHALL Candidate'e gerçek zamanlı başvuru istatistikleri gösteren bir dashboard sunmalıdır
2. WHEN dashboard açıldığında, THEN System SHALL toplam başvuru sayısını, yanıt oranını, mülakat davetlerini ve red sayısını göstermelidir
3. THE System SHALL başvuru durumlarını (Beklemede, Gönderildi, Görüldü, Yanıt Alındı, Reddedildi) görsel olarak sunmalıdır
4. WHEN yeni bir durum güncellemesi olduğunda, THEN System SHALL Candidate'e gerçek zamanlı bildirim göndermelidir
5. THE System SHALL haftalık ve aylık başvuru trendlerini grafik olarak göstermelidir

### Gereksinim 11: Özgeçmiş Parse ve Yapılandırma

**Kullanıcı Hikayesi:** Aday olarak, farklı formatlardaki özgeçmişlerimi sisteme yükleyebilmek istiyorum.

#### Kabul Kriterleri

1. THE System SHALL PDF, DOCX ve TXT formatlarındaki özgeçmişleri parse edebilmelidir
2. WHEN özgeçmiş parse edildiğinde, THEN System SHALL kişisel bilgileri, eğitim geçmişini, iş deneyimlerini, becerileri ve sertifikaları çıkarmalıdır
3. WHEN parse işlemi tamamlandığında, THEN System SHALL çıkarılan verileri yapılandırılmış JSON formatında saklamalıdır
4. IF parse işlemi başarısız olursa, THEN System SHALL Candidate'e hata mesajı göstermeli ve manuel düzenleme seçeneği sunmalıdır
5. THE Gemini_Engine SHALL belirsiz veya eksik bilgileri tespit etmeli ve Candidate'den açıklama istemelidir

### Gereksinim 12: Şirket ve Kültür Analizi

**Kullanıcı Hikayesi:** Aday olarak, başvurduğum şirketler hakkında detaylı bilgi almak istiyorum.

#### Kabul Kriterleri

1. WHEN Job_Posting sisteme eklendiğinde, THEN Gemini_Engine SHALL şirketin web sitesini, blog yazılarını ve sosyal medya içeriklerini taramalıdır
2. WHEN şirket analizi tamamlandığında, THEN Gemini_Engine SHALL şirket kültürü, değerleri ve çalışma ortamı hakkında özet üretmelidir
3. THE System SHALL şirket analiz sonuçlarını Job_Posting ile ilişkilendirerek saklamalıdır
4. WHEN Candidate bir ilanı görüntülediğinde, THEN System SHALL şirket analiz özetini sunmalıdır
5. THE Gemini_Engine SHALL şirketin son haberlerini ve gelişmelerini analiz ederek Candidate'e içgörü sağlamalıdır

### Gereksinim 13: Çok Dilli Destek

**Kullanıcı Hikayesi:** Aday olarak, platformu Türkçe ve İngilizce dillerinde kullanabilmek istiyorum.

#### Kabul Kriterleri

1. THE System SHALL Türkçe ve İngilizce dil desteği sunmalıdır
2. WHEN Candidate dil değiştirdiğinde, THEN System SHALL tüm arayüz metinlerini seçilen dilde göstermelidir
3. WHEN Gemini_Engine içerik üretirken, THEN System SHALL Candidate'in tercih ettiği dili kullanmalıdır
4. THE System SHALL iş ilanlarını orijinal dilinde saklamalı ancak talep üzerine çeviri sunmalıdır
5. WHEN Tailored_Resume oluşturulurken, THEN Gemini_Engine SHALL Job_Posting'in diline uygun özgeçmiş üretmelidir

### Gereksinim 14: API Entegrasyonları

**Kullanıcı Hikayesi:** Sistem yöneticisi olarak, platformun dış servislerle güvenli entegrasyon yapmasını istiyorum.

#### Kabul Kriterleri

1. THE System SHALL Google Gemini API ile güvenli iletişim kurmalıdır
2. THE System SHALL Gmail API üzerinden e-posta gönderimi yapabilmelidir
3. THE System SHALL AWS Cognito ile kullanıcı kimlik doğrulaması yapmalıdır
4. THE System SHALL AWS S3'e dosya yükleme ve indirme işlemleri yapabilmelidir
5. WHEN API çağrısı başarısız olduğunda, THEN System SHALL otomatik yeniden deneme (retry) mekanizması uygulamalıdır
6. THE System SHALL tüm API çağrılarını loglayarak hata ayıklama için kayıt tutmalıdır

### Gereksinim 15: Performans ve Ölçeklenebilirlik

**Kullanıcı Hikayesi:** Sistem yöneticisi olarak, platformun yüksek kullanıcı yükünde bile performanslı çalışmasını istiyorum.

#### Kabul Kriterleri

1. THE System SHALL 10,000 eşzamanlı kullanıcıyı desteklemelidir
2. WHEN Job_Posting taraması yapılırken, THEN System SHALL paralel işleme kullanarak performansı optimize etmelidir
3. THE System SHALL API yanıt sürelerini 2 saniyenin altında tutmalıdır
4. WHEN User_Database sorgulanırken, THEN System SHALL pgvector indeksleme kullanarak semantik aramaları hızlandırmalıdır
5. THE System SHALL yük dengeleme (load balancing) kullanarak trafiği dağıtmalıdır

### Gereksinim 16: Geri Bildirim ve Öğrenme Sistemi

**Kullanıcı Hikayesi:** Aday olarak, sistemin bana sunduğu eşleşmeleri "beğenmedim" olarak işaretleyebilmek ve nedenini belirtebilmek istiyorum, böylece Dijital İkizim zamanla tercihlerimi daha iyi öğrenebilir.

#### Kabul Kriterleri

1. WHEN Candidate bir ilanı sola kaydırdığında (red), THEN System SHALL red nedeni için kısa bir anket sunmalıdır
2. THE System SHALL red nedenleri olarak "Maaş düşük", "Teknoloji eski", "Lokasyon uygun değil", "Şirket kültürü uymuyor" ve "Diğer" seçeneklerini sunmalıdır
3. WHEN Candidate red nedeni belirttiğinde, THEN System SHALL geri bildirimi Digital_Twin ile ilişkilendirerek kaydetmelidir
4. WHEN geri bildirimler toplandığında, THEN Gemini_Engine SHALL Digital_Twin üzerindeki ağırlık katsayılarını (weights) güncellemelidir
5. THE System SHALL en az 10 geri bildirim toplandıktan sonra öğrenme modelini aktif hale getirmelidir

### Gereksinim 17: Yetenek Boşluğu Analizi

**Kullanıcı Hikayesi:** Aday olarak, %100 eşleşmediğim ilanlarda hangi becerilerimin eksik olduğunu ve bu açığı nasıl kapatabileceğime dair öneriler almak istiyorum.

#### Kabul Kriterleri

1. WHEN Match_Score < 100 olduğunda, THEN Gemini_Engine SHALL eksik becerileri ve anahtar kelimeleri listelemelidir
2. WHEN yetenek boşluğu tespit edildiğinde, THEN System SHALL eksiklikleri kategorize ederek (Teknik Beceri, Sertifika, Deneyim) sunmalıdır
3. WHEN eksiklikler listendiğinde, THEN Gemini_Engine SHALL her eksiklik için online kurs önerileri (Coursera, Udemy, LinkedIn Learning) üretmelidir
4. THE Gemini_Engine SHALL pratik proje önerileri sunarak Candidate'in portföyünü güçlendirmesine yardımcı olmalıdır
5. WHEN Candidate bir öğrenme kaynağını tamamladığında, THEN System SHALL Digital_Twin'i güncellemeli ve yeni Match_Score hesaplamalıdır

### Gereksinim 18: Şeffaf İşlem Günlüğü

**Kullanıcı Hikayesi:** Aday olarak, tarayıcı eklentisinin LinkedIn üzerinde hangi formları doldurduğunu, hangi butonlara tıkladığını ve süreci nasıl tamamladığını adım adım takip etmek istiyorum.

#### Kabul Kriterleri

1. THE Browser_Extension SHALL her başvuru sırasında yaptığı kritik işlemlerin (Input fill, Click, Submit) zaman damgalı loglarını tutmalıdır
2. WHEN Browser_Extension bir işlem gerçekleştirdiğinde, THEN System SHALL işlem tipini, hedef elementi ve durumu kaydetmelidir
3. WHEN başvuru tamamlandığında, THEN System SHALL tüm işlem loglarını Candidate'e kronolojik sırayla sunmalıdır
4. IF Browser_Extension hata aldığında, THEN System SHALL hatanın ekran görüntüsünü (screenshot) almalı ve hata detaylarıyla birlikte kaydetmelidir
5. THE System SHALL Candidate'in son 30 günlük başvuru loglarını erişilebilir tutmalıdır

### Gereksinim 19: LinkedIn Profil Optimizasyonu

**Kullanıcı Hikayesi:** Aday olarak, LinkedIn profilimin güncel iş piyasası trendlerine ve hedeflediğim pozisyonlara ne kadar uyumlu olduğunu analiz edilmesini ve iyileştirme önerileri sunulmasını istiyorum.

#### Kabul Kriterleri

1. WHEN Candidate LinkedIn profil URL'si sağladığında, THEN Gemini_Engine SHALL profili tarayarak "Başlık", "Hakkında" ve "Deneyim" kısımlarını analiz etmelidir
2. WHEN profil analizi tamamlandığında, THEN Gemini_Engine SHALL her bölüm için SEO uyumlu ve ATS (Applicant Tracking System) dostu metin önerileri üretmelidir
3. THE Gemini_Engine SHALL hedeflenen pozisyonlara göre anahtar kelime optimizasyonu önermelidir
4. WHEN optimizasyon önerileri sunulduğunda, THEN System SHALL orijinal ve önerilen metinleri yan yana karşılaştırmalı olarak göstermelidir
5. THE System SHALL profil güçlendirme skoru (0-100) hesaplayarak Candidate'e ilerleme göstermelidir

### Gereksinim 20: Manuel Müdahale ve Taslak Modu

**Kullanıcı Hikayesi:** Aday olarak, otomatik gönderim yapılmadan önce Gemini tarafından hazırlanan "Kişiselleştirilmiş E-posta" veya "Kapak Mektubu" üzerinde manuel düzenleme yapabilmek istiyorum.

#### Kabul Kriterleri

1. WHEN Candidate bir başvuruyu onayladığında, THEN System SHALL gönderimden önce bir "Düzenleme Ekranı" (Preview & Edit) açmalıdır
2. THE System SHALL Gemini tarafından üretilen Tailored_Resume, kapak mektubu ve e-posta içeriğini düzenlenebilir formatta sunmalıdır
3. WHEN Candidate içeriği düzenlediğinde, THEN System SHALL değişiklikleri gerçek zamanlı olarak kaydetmelidir
4. THE System SHALL Candidate içeriği onaylamadan Browser_Extension veya Gmail_API sürecini başlatmamalıdır
5. WHEN Candidate "Gönder" butonuna tıkladığında, THEN System SHALL son onay için özet ekranı göstermeli ve ardından başvuruyu göndermelidir

### Gereksinim 21: Doğrulanmış Şirket Veritabanı

**Kullanıcı Hikayesi:** Aday olarak, sahte ilanlarla vakit kaybetmemek için sadece doğrulanmış şirketlere başvuru yapmak istiyorum.

#### Kabul Kriterleri

1. THE System SHALL minimum 1247 doğrulanmış şirketin bilgilerini içeren Verified_Company_Database tutmalıdır
2. WHEN şirket veritabanına yeni şirket eklendiğinde, THEN System SHALL şirketin web sitesi, vergi numarası ve iletişim bilgilerini doğrulamalıdır
3. THE Verified_Company_Database SHALL her şirket için gerçek İK iletişim bilgilerini (e-posta, telefon) içermelidir
4. WHEN Job_Posting tarandığında, THEN System SHALL ilanı sadece Verified_Company_Database'de bulunan şirketlerle eşleştirmelidir
5. IF ilan doğrulanmamış bir şirkete aitse, THEN System SHALL ilanı işaretlemeli ve Candidate'e uyarı göstermelidir

### Gereksinim 22: Sektörel ve Coğrafi Filtreleme

**Kullanıcı Hikayesi:** Aday olarak, ilgilendiğim sektörlerde ve çalışmak istediğim şehirlerde iş aramak istiyorum.

#### Kabul Kriterleri

1. THE System SHALL minimum 14 farklı sektör kategorisi (Teknoloji, Finans, Sağlık, E-ticaret, vb.) sunmalıdır
2. WHEN Candidate profil oluştururken, THEN System SHALL tercih edilen sektörleri seçme imkanı vermelidir
3. THE System SHALL Türkiye'deki tüm şehirler için Geographic_Filter desteği sunmalıdır
4. WHEN Candidate şehir tercihi belirlediğinde, THEN System SHALL sadece o şehirlerdeki ilanları taramalıdır
5. THE System SHALL Candidate'in birden fazla sektör ve şehir seçmesine izin vermelidir
6. WHEN Job_Posting tarandığında, THEN System SHALL ilanı Sector_Filter ve Geographic_Filter kriterlerine göre filtrelemelidir

### Gereksinim 23: Basitleştirilmiş Başvuru Akışı

**Kullanıcı Hikayesi:** Aday olarak, karmaşık formlar doldurmadan hızlı ve kolay bir şekilde başvuru yapmak istiyorum.

#### Kabul Kriterleri

1. THE System SHALL 4 adımlı basit bir başvuru akışı sunmalıdır: (1) Google ile giriş, (2) Filtreleme, (3) CV/Mesaj ekleme, (4) Gönderim onayı
2. WHEN Candidate ilk kez giriş yaptığında, THEN System SHALL Google OAuth ile kimlik doğrulaması yapmalıdır
3. WHEN Candidate CV yüklediğinde, THEN System SHALL CV'yi bir kez saklayarak sonraki başvurularda otomatik kullanmalıdır
4. THE System SHALL Candidate'in her başvuru için özel mesaj eklemesine izin vermelidir
5. WHEN tüm adımlar tamamlandığında, THEN System SHALL özet ekranı göstermeli ve tek tıkla gönderim yapmalıdır

### Gereksinim 24: Güvenli ve Kontrollü Gönderim

**Kullanıcı Hikayesi:** Aday olarak, başvurularımın güvenli bir şekilde ve benim kontrolümde gönderilmesini istiyorum.

#### Kabul Kriterleri

1. THE System SHALL hiçbir başvuruyu Candidate onayı olmadan göndermemelidir
2. WHEN Candidate toplu başvuru onayladığında, THEN System SHALL başvuruları sıralı bir şekilde (sequential) göndermelidir
3. THE System SHALL her başvuru arasında minimum 5 dakika bekleme süresi uygulamalıdır
4. WHEN başvuru gönderilirken, THEN System SHALL Candidate'in kendi Gmail hesabını kullanmalıdır
5. THE System SHALL gönderim sırasında hata oluşursa Candidate'i bilgilendirmeli ve yeniden deneme seçeneği sunmalıdır
6. WHEN başvuru başarıyla gönderildiğinde, THEN System SHALL gönderim onayını ve zaman damgasını kaydetmelidir
