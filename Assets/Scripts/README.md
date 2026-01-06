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
├── UI/                    # Kullanıcı arayüzü
│   ├── SkillUIManager.cs  # UI panel yöneticisi
│   └── SkillButton.cs     # Skill seçim butonları
├── ExerciseManager.cs     # Exercise modu ve zone-based training
├── MovingObstacle.cs      # Hareketli engeller (Skill 30)
└── RealtimeCoachTutorial.cs  # Tutorial sistemi
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

#### ExerciseManager (YENİ)
- Exercise butonu ile skill seçim UI'ını açar
- Zone-based training sistemini yönetir
- 10 farklı skill için 4 training zone'u destekler:
  - Basic Movement Zone (Skill 1-5): İleri, geri, dönüşler
  - Incline Zone (Skill 15-16): 5° eğim
  - Curb Zone (Skill 25-26): 15cm kaldırım
  - Obstacle Zone (Skill 30): Hareketli engeller
- **Turn Skill Direction Selection**: Skills 3, 4, 5, 28 için yön seçimi UI'ı
  - Kullanıcı "LEFT" veya "RIGHT" seçebilir
  - Backend'e yöne özel soru gönderilir
  - GPT doğru action'ı (turn_left/turn_right) üretir
- RAG sisteminden tutorial adımları alır
- Wheelchair'ı doğru zone'a teleport eder
- RealtimeCoachTutorial ile entegre çalışır

#### SkillButton (YENİ)
- Skill seçim UI'ındaki butonlar için kullanılır
- Her butona bir skill ID atanır
- Tıklandığında ExerciseManager.OnSkillSelected() çağırır

#### MovingObstacle (YENİ)
- Hareketli engeller için waypoint sistemi
- Ping-pong veya loop mod destekler
- Ayarlanabilir hız
- Unity Editor'de görsel gizmos ile waypoint'leri gösterir

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

### Temel API Kullanımı
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

### Exercise Manager Kullanımı (YENİ)

#### Unity Inspector'da Kurulum:
1. Bir GameObject oluşturun ve `ExerciseManager` component'ini ekleyin
2. UI referanslarını atayın:
   - Main Menu Panel
   - Skill Selection Panel
   - Exercise Button
3. **Direction Selection Panel** (YENİ):
   - Direction Selection Panel (Container GameObject)
   - Turn Left Button (Button component)
   - Turn Right Button (Button component)
4. Zone Transform'larını atayın:
   - Basic Movement Zone
   - Incline Zone
   - Curb Zone
   - Obstacle Zone
5. Wheelchair ve RealtimeCoachTutorial referanslarını atayın

#### Skill Button Kurulumu:
1. Skill seçim UI'ınızda her skill için bir Button oluşturun
2. Her Button'a `SkillButton` component'ini ekleyin
3. Inspector'da skill ID'yi set edin (örn: "1", "15", "25")
4. ExerciseManager referansını atayın (veya otomatik bulunur)

#### Direction Selection Panel Kurulumu (YENİ):
Unity Editor'da yeni bir UI paneli oluşturun:

```
Canvas (veya mevcut Canvas)
└── DirectionSelectionPanel (Panel)
    ├── Background (Image - semi-transparent black)
    ├── TitleText (Text)
    │   └── Text: "Select Turn Direction"
    ├── LeftButton (Button)
    │   └── Text: "← LEFT (A)"
    └── RightButton (Button)
        └── Text: "RIGHT (D) →"
```

**Detaylı Adımlar:**
1. Canvas üzerinde yeni bir **Panel** oluşturun (sağ tık → UI → Panel)
2. Adını **DirectionSelectionPanel** yapın
3. Panel'in background Image component'inde Alpha değerini 0.8-0.9 yapın (semi-transparent)
4. Panel içine bir **Text** ekleyin:
   - Adı: TitleText
   - Text: "Select Turn Direction"
   - Font size: 24-32
5. İki **Button** ekleyin:
   - **LeftButton**: 
     - Child Text: "← LEFT (A)"
     - Position: Sol tarafta
   - **RightButton**: 
     - Child Text: "RIGHT (D) →"
     - Position: Sağ tarafta
6. **ExerciseManager** Inspector'da:
   - Direction Selection Panel → DirectionSelectionPanel GameObject
   - Turn Left Button → LeftButton Button component
   - Turn Right Button → RightButton Button component
7. Panel'i başlangıçta **inactive** yapın (checkbox'ı kaldırın)

**Not:** Button event'leri kod tarafından otomatik olarak bağlanır, Unity Editor'da OnClick olaylarını manuel bağlamanıza gerek yok.

#### Kullanım:
```csharp
// Exercise butonuna tıklandığında
exerciseManager.OpenSkillSelection();

// Skill seçildiğinde (otomatik olarak çağrılır)
exerciseManager.OnSkillSelected("15"); // Skill 15: Ascends 5° incline

// Turn skill seçildiğinde (Skills 3, 4, 5, 28)
exerciseManager.OnSkillSelected("3");  // Direction selection panel açılır
// Kullanıcı "LEFT" veya "RIGHT" seçer
// Backend'e yöne özel soru gönderilir:
// "How do I turn left 90 degrees while moving forward in a wheelchair?"
```

**Turn Skills User Flow:**
1. Kullanıcı "Skill 3: Turn while moving forward" seçer
2. Direction Selection Panel otomatik açılır
3. Kullanıcı "LEFT (A)" veya "RIGHT (D)" butonuna tıklar
4. Backend'e spesifik soru gider: "How do I turn LEFT 90 degrees..."
5. GPT doğru action'ı döndürür: `turn_left` 
6. Tutorial başlar ve kullanıcıya 'A' tuşuna basması söylenir

#### Moving Obstacle Kurulumu (Skill 30 için):
1. Bir GameObject oluşturun ve `MovingObstacle` component'ini ekleyin
2. Boş GameObjects oluşturup waypoint'ler olarak kullanın
3. Waypoint'leri MovingObstacle'ın waypoints listesine ekleyin
4. Speed değerini ayarlayın
5. Ping-pong veya loop modu seçin
