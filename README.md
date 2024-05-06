# Facade Analysis Plugin for Rhino

## Genel Bakış
Facade Analysis, Rhino için geliştirilmiş bir .NET eklentisidir. Bu eklenti, mimari modeller üzerinde cephe analizleri yapmanızı, normal rhino komutuna ek olarak tüm cepheyi eş zamanlı ve tek komut satırı ile analiz etmenizi sağlar. Böylece modellerin farklı geometrik ve fiziksel özelliklerini değerlendirebilirsiniz.

## Başlarken

Bu bölüm, Facade Analysis eklentisini Visual Studio üzerinden nasıl kurup Rhino içerisine nasıl entegre edeceğinizi adım adım anlatmaktadır.

### Gereksinimler

- Rhino 6 veya üstü
- Visual Studio 2017 veya üstü
- .NET Framework 4.6.1 veya üstü

### Kurulum Adımları

#### 1. Projeyi İndirme
Projeyi GitHub reposundan klonlayın veya zip olarak indirin:
git clone https://github.com/yourusername/FacadeAnalysis.git

#### 2. Visual Studio ile Açma
İndirdiğiniz projeyi Visual Studio'da açın. Çözüm gezgini içerisinde, `FacadeAnalysis.sln` dosyasını çift tıklayarak projeyi açın.

#### 3. Proje İnşa Etme
Visual Studio'da, `Build > Build Solution` seçeneğini kullanarak projeyi derleyin. Başarılı bir şekilde derlendikten sonra, projenin bulunduğu dizinde `bin/Debug` veya `bin/Release` klasörü altında `.dll` dosyası oluşacaktır.

### Rhino'ya Eklenti İmport Etme

Oluşan `.dll` dosyasını Rhino'ya eklemek için aşağıdaki adımları izleyin:

1. Rhino'yu açın.
2. `Tools > Options` menüsünden `Plug-ins` sekmesini açın.
3. `Install` butonuna tıklayarak yeni bir eklenti yüklemek için diyalog penceresini açın.
4. Eklenti olarak yüklemek istediğiniz `.dll` dosyasını bulun ve seçin.
5. `Aç/Open` butonuna tıklayarak eklentiyi yükleyin.
6. Rhino'yu yeniden başlatın.

Eklenti başarıyla yüklendikten sonra, Rhino arayüzünde `FacadeAnalysis` komutlarını kullanmaya başlayabilirsiniz.

## Kullanım

Eklentiyi yükledikten sonra, Rhino komut satırına `RunFacadeAnalysis` yazarak eklentiyi çalıştırabilirsiniz. Bu komut, kullanıcı arayüzü üzerinden çeşitli cephe analizi işlemlerini başlatmanıza olanak tanır.

## Katkıda Bulunma

Projeye katkıda bulunmak isterseniz, lütfen öncelikle bir issue açarak veya mevcut issue'lar üzerinden gitmek üzere bir pull request oluşturun.
