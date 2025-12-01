# Wheelchair Skills RAG Integration Scripts

Bu klasör, Unity projesinin RAG (Retrieval-Augmented Generation) backend sistemiyle haberleşmesini sağlayan C# scriptlerini içerir.

## Klasör Yapısı

```
Scripts/
├── API/                    # Backend API entegrasyonu
│   ├── APIClient.cs       # HTTP istemci (Singleton)
│   ├── APIModels.cs       # JSON veri modelleri
│   └── APIEndpoints.cs    # Backend URL ve endpoint tanımları
├── SkillTraining/         # Beceri eğitimi yönetimi
│   ├── InputMapping.cs    # KeyCode <-> Action eşlemesi
│   ├── SkillAttemptTracker.cs  # Ana deneme yöneticisi
│   └── InputRecorder.cs   # Kullanıcı input dinleyici
└── UI/                    # Kullanıcı arayüzü
    └── SkillUIManager.cs  # UI panel yöneticisi
```

## Kullanım

### 1. Backend URL Ayarlama

`APIEndpoints.cs` dosyasında backend URL'ini ayarlayın:

```csharp
public static string BaseURL = "http://localhost:8000";
```

### 2. Temel Kurulum

1. Bir GameObject oluşturun ve `SkillAttemptTracker` component'ini ekleyin
2. Aynı GameObject'e `InputRecorder` component'ini ekleyin
3. UI için bir Canvas oluşturun ve `SkillUIManager` component'ini ekleyin
4. `SkillUIManager`'da gerekli UI referanslarını atayın

### 3. Script Detayları

#### APIClient (Singleton)
- Otomatik olarak oluşturulur ve sahneler arası kalıcıdır
- Tüm HTTP isteklerini yönetir
- Coroutine tabanlı async işlemler

#### SkillAttemptTracker
- Deneme durumunu yönetir
- API çağrılarını koordine eder
- Event'ler aracılığıyla UI'ı bilgilendirir

#### InputRecorder
- Kullanıcı tuş basışlarını dinler
- `InputMapping` kullanarak backend action'larına çevirir
- Metadata ile zenginleştirilmiş input kayıtları

#### SkillUIManager
- Beceri seçim ekranını yönetir
- Eğitim sırasında UI'ı günceller
- Sonuç ekranını gösterir

## Event'ler

`SkillAttemptTracker` şu event'leri sağlar:

- `OnAttemptStarted`: Deneme başladığında
- `OnAttemptEnded`: Deneme sonlandığında
- `OnInputRecorded`: Her input kaydedildiğinde
- `OnError`: Hata oluştuğunda

## API Endpoint'leri

Backend aşağıdaki endpoint'leri desteklemelidir:

- `GET /skills` - Mevcut becerileri listele
- `GET /skills/{id}` - Belirli bir beceriyi getir
- `POST /attempts/start` - Yeni deneme başlat
- `POST /attempts/record_input` - Input kaydet
- `POST /attempts/end` - Denemeyi sonlandır
- `GET /attempts/{id}/feedback` - Deneme geri bildirimi al
- `POST /rag/hint` - Dinamik ipucu iste
- `POST /rag/help` - Bağlamsal yardım iste

## Notlar

- Tüm API çağrıları asenkron (Coroutine tabanlı)
- JSON serileştirme için Unity'nin `JsonUtility` kullanılıyor
- Uygulama kapatıldığında aktif denemeler otomatik sonlandırılır
- `APIClient` DontDestroyOnLoad ile kalıcı hale getirilmiştir

## Örnek Kullanım

```csharp
// Deneme başlatma
attemptTracker.StartAttempt("skill_wheelchair_forward");

// Manuel input kaydı
attemptTracker.RecordInput("move_forward");

// Denemeyi sonlandırma
attemptTracker.EndAttempt("completed");

// İpucu isteme
attemptTracker.RequestHint("stuck_at_obstacle", response => {
    Debug.Log(response.hint);
});
```
