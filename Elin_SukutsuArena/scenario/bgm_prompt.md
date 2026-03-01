
---

## **📊 カテゴリ別統計**

| カテゴリー | 実装済み | 未実装 | 合計 |
|----------|---------|--------|------|
| オープニング・ロビー | 2曲 | 0曲 | 2曲 |
| 不穏・緊張系 | 3曲 | 1曲 | 4曲 |
| 戦闘BGM（ランク戦） | 3曲 | 1曲 | 4曲 |
| 特殊戦闘・ボス戦 | 7曲 | 0曲 | 7曲 |
| リリィテーマ | 5曲 | 0曲 | 5曲 |
| ゼクテーマ | 2曲 | 1曲 | 3曲 |
| ストーリー・感情 | 6曲 | 1曲 | 7曲 |
| 最終章専用 | 4曲 | 0曲 | 4曲 |
| 環境・演出 | 2曲 | 1曲 | 3曲 |
| **総計** | **34曲** | **5曲** | **39曲** |

**実装進捗率**: 87.2% (34/39)

---

## **🎮 シーン別BGM対応表**

| シナリオファイル | メインBGM | サブBGM |
|-----------------|----------|---------|
| 01_opening.md | `sukutsu_arena_opening.mp3` → `Lobby_Normal.mp3` | `Ominous_Suspense_01.mp3` |
| 02_rank_up_01.md | ~~`Battle_RankF_Mud.mp3`~~ (未実装) | `Ominous_Suspense_02.mp3` |
| 03_zek.md | `Zek_Merchant.mp3` | `Ominous_Suspense_01.mp3` |
| 04_rank_up_02.md | `Battle_RankE_Ice.mp3` | `Emotional_Sorrow.mp3` |
| 05_1_lily_experiment.md | `Lily_Tranquil.mp3` | ~~`Crafting_Ambient.mp3`~~ (未実装) |
| 05_2_zek_steal_lily.md | `Ominous_Suspense_02.mp3` | `Zek_Merchant_2.mp3` |
| 06_1_rank_up_03.md | `Battle_Kain_Requiem.mp3` | `Emotional_Sorrow_2.mp3` |
| 06_2_zek_steal_soulgem.md | `Ominous_Suspense_02.mp3` | `Zek_Merchant.mp3` |
| 07_upper_existence.md | `Battle_RankD_Chaos.mp3` | `Fanfare_Audience.mp3` |
| 08_lily_private.md | `Lily_Private_Room.mp3` | `Lily_Seductive_Danger.mp3` |
| 09_bulgas_training.md | `Battle_Balgas_Training.mp3` | ~~`Emotional_Warmth.mp3`~~ (未実装) |
| 10_rank_up_04.md | `Battle_RankC_Heroic_Lament.mp3` | `Emotional_Sorrow.mp3` |
| 11_rank_up_05.md | `Battle_Null_Assassin.mp3` | `Ominous_Heartbeat.mp3` |
| 12_makuma.md | `Lily_Seductive_Danger.mp3` | `Ominous_Suspense_01.mp3` |
| 13_makuma2.md | ~~`Ominous_Rumble.mp3`~~ (未実装) | `Ominous_Heartbeat.mp3` |
| 14_rank_up_06.md | `Battle_Shadow_Self.mp3` | `Ominous_Heartbeat.mp3` |
| 15_vs_bulgas.md | `Battle_Balgas_Prime.mp3` | `Emotional_Sacred_Triumph_Special.mp3` |
| 16_lily_real_name.md | `Lily_Confession.mp3` / `Lily_Confession_2.mp3` | `Lily_Private_Room.mp3` |
| 17_vs_grandmaster_1.md | `Final_Astaroth_Throne.mp3` | ~~`Zek_Hideout.mp3`~~ (未実装) |
| 18_last_battle.md | `Battle_Astaroth_Phase1.mp3` → `Battle_Astaroth_Phase2.mp3` | `Ending_Celebration.mp3` |

---

---

# 以下、AI作曲プロンプト詳細

AI作曲家への入力用プロンプト集

---

## **BGM一覧（AI作曲プロンプト形式）**

### **1. ロビー・通常BGM**

#### **Lobby_Normal**
```
Ambient dark fantasy tavern theme, 80-90 BPM. Medieval tavern atmosphere with distant chains rattling, metal creaking. Deep bass undertones, sparse melancholic strings. Mysterious and melancholic ambience. Low tension background music for a cursed arena lobby.
```

**使用シーン**: ロビー通常時、受付での会話

---

### **2. 不穏・緊張系BGM**

#### **Ominous_Tension_01 (ゼク登場)**
```
Sudden silence transitioning to dissonant low drones and noise-like textures, 60 BPM. Unsettling string dissonance, whisper-like choral pads in the background. Glitchy metallic sound effects. Creates sense of dimensional distortion and malevolent presence.
```

**使用シーン**: ゼクの初登場、影からの囁き、次元の歪み

---

#### **Ominous_Suspense_02 (選択迫る)**
```
Tense suspense theme with clock ticking sounds, 70 BPM. Dissonant strings building tension, heartbeat-like bass drum. Sharp staccato violins, dark ambient pads. Psychological pressure and moral dilemma atmosphere. Peaks with dramatic string crescendos.
```

**使用シーン**: 重大な選択肢の提示、ゼクの取引提案、道徳的ジレンマ

---

#### **Ominous_Heartbeat (危機・告白)**
```
Dark ambient with pronounced heartbeat rhythm, 72 BPM (heartbeat tempo). Heavy sub-bass pulses mimicking heartbeat, minimal melody. Tension-building string sustains, unsettling atmospheric drones. Claustrophobic and intimate emotional pressure.
```

**使用シーン**: リリィとの対立、バルガスへの告白、内面の葛藤

---

#### **Ominous_Rumble (地鳴り・崩壊)**
```
Low ominous rumbling theme, 50 BPM. Deep sub-bass earth-shaking drones, distant earthquake sounds. Cracking stone SFX, dimensional instability. Building sense of imminent collapse. Heavy and oppressive atmosphere.
```

**使用シーン**: 虚空の心臓の脈動、アリーナの崩壊兆候、次元の不安定化

---

### **3. 戦闘BGM（通常ランク戦）**

#### **Battle_RankF_Mud (泥犬)**
```
Dark arena battle theme, 120 BPM. Dissonant strings and aggressive taiko drum patterns. Driving 4/4 rhythm with chains rattling SFX. Medieval combat intensity with desperate, survival-focused energy. Metallic percussion, low brass stabs.
```

**使用シーン**: Rank F 初戦、泥の闘技場

---

#### **Battle_RankE_Ice (凍土の試練)**
```
Frozen arena battle theme, 115 BPM. Ice bell-like tones (glass harmonica/celesta), crystalline synth textures. Blizzard wind SFX layered throughout. Cold strings, sharp metallic impacts. Elegant yet deadly ice combat atmosphere.
```

**使用シーン**: Rank E 昇格試合、氷の闘技場

---

#### **Battle_RankD_Chaos (観客の供物)**
```
Chaotic battle with falling objects hazard, 135 BPM. Driving combat rhythm interrupted by random crash/impact SFX. Unpredictable percussion patterns, dissonant brass stabs. Frantic strings, glitchy electronic elements. Constant danger and environmental chaos.
```

**使用シーン**: Rank D 初戦、観客のヤジ（物理）、落下物ギミック

---

#### **Battle_RankC_Heroic_Lament (英雄の墓標)**
```
Sorrowful battle against hero remnants, 110 BPM. Melancholic orchestral combat theme. Tragic string melodies over heavy war drums. Distant bell tolls, ghostly choir pads. Fighting fallen heroes - respectful yet intense. Low growling undertones.
```

**使用シーン**: Rank C 初戦、英雄の残党との戦い

---

### **4. 特殊戦闘BGM**

#### **Battle_Balgas_Training (師匠の特訓)**
```
Sparring/training battle theme, 125 BPM. Heavy orchestral drums and determined brass. Strong rhythmic foundation, mentor-student dynamic energy. Martial percussion, grunting warrior vocalizations. Honorable combat atmosphere, building to crescendos.
```

**使用シーン**: Rank C 昇格試合、バルガスの哲学特訓

---

#### **Battle_Kain_Requiem (カインの鎮魂)**
```
Mournful boss battle theme, 105 BPM. Sorrowful orchestral combat with tragic melody. Heavy heartbeat-like drum, melancholic strings. Rusted metal grinding SFX. Fighting to grant peace - sad yet determined. Blue flame-like ethereal choir backing.
```

**使用シーン**: Rank E 昇格試合、カインとの戦い

---

#### **Battle_Null_Assassin (虚無の処刑人)**
```
Stealth horror boss battle, 140 BPM. Starts with complete silence transitioning to only heartbeat sounds. Minimal instrumentation - heartbeat bass drum, sharp metallic blade sounds. Sudden violent string strikes. Creates paranoia and fear of unseen assassin. No melody, pure tension.
```

**使用シーン**: Rank B 昇格試合、ヌルとの戦い

---

#### **Battle_Shadow_Self (鏡像の処刑)**
```
Psychological boss battle against doppelganger, 128 BPM. Heavy ominous orchestral theme with distorted mirror-like sound effects. Dark heroic brass twisted into dissonance. Heartbeat percussion, glitchy reversed audio. Fighting one's own darkness - introspective and intense.
```

**使用シーン**: Rank A 昇格試合、ドッペルゲンガー・シャドウとの戦い

---

#### **Battle_Balgas_Prime (全盛期の幻影)**
```
Epic mentor battle theme, 145 BPM. Heavy orchestral combat with sword clashing SFX. Heroic yet sorrowful brass, pounding war drums. Master vs student intensity. Driving strings, emotional peaks and valleys. Honor and desperation combined.
```

**使用シーン**: Rank S 昇格試合、全盛期バルガスとの戦い

---

### **5. ボスBGM（最高難度）**

#### **Battle_Astaroth_Phase1 (権能付き)**
```
Godlike boss battle theme, 180 BPM. Massive orchestral and choir arrangement. Oppressive symphonic metal elements, cathedral organ backing. Heartbeat bass drum emphasizing overwhelming power. Reality-warping glitch effects. Divine judgment atmosphere. Level 100 million entity presence.
```

**使用シーン**: 最終決戦 第1フェーズ、アスタロトの権能発動中

---

#### **Battle_Astaroth_Phase2 (権能解除後)**
```
Climactic final battle theme, 158 BPM. Epic orchestral metal hybrid at maximum intensity. Heroic brass fanfares vs dark choir. Pounding cinematic percussion, shredding string ostinatos. Hope vs despair musical conflict. Universe-shaking scale. Building to ultimate crescendo.
```

**使用シーン**: 最終決戦 第2フェーズ、仲間が権能を解除した後

---

### **6. リリィ・キャラクターテーマ**

#### **Lily_Seductive_Danger (受付の妖艶)**
```
Seductive succubus theme, 85 BPM. Sultry jazz-influenced dark orchestral. Breathy female vocal pads, purple smoke-like ambient textures. Sensual strings, distant chains rattling. Elegant harpsichord, seductive yet dangerous atmosphere. Sophisticated predator allure.
```

**使用シーン**: リリィの私室、妖艶なシーン、サキュバスの本性

---

#### **Lily_Private_Room (紫煙と観察者)**
```
Intimate succubus chamber theme, 75 BPM. Deep sensual ambience with purple haze textures. Low sultry strings, distant heartbeat bass. Mysterious chimes, breathing-like pads. Intoxicating and dangerous intimacy. Magic circuit humming undertones.
```

**使用シーン**: リリィの私室（深夜）、親密なシーン、捕食者の本能

---

#### **Lily_Confession (真名の刻印)**
```
Sacred romantic confession theme, 70 BPM. Beautiful mystical orchestral ballad. Ethereal choir, magical chime textures. Emotional strings building to climax. Runic sound effects (crystalline tones). Divine contract ceremony atmosphere. Heartbeat synchronized with music.
```

**使用シーン**: リリィの真名開示、契約の接吻、永遠の共犯

---

#### **Lily_Tranquil (穏やかな安らぎ)**
```
Gentle peaceful theme, 65 BPM. Soft strings and warm piano. Calming atmosphere with subtle magic shimmer. Tender and protective mood. Relief after battle, quiet intimacy. Succubus showing genuine care.
```

**使用シーン**: リリィとの穏やかな会話、報酬授与後、優しいシーン

---

### **7. ゼク・キャラクターテーマ**

#### **Zek_Merchant (剥製師の美学)**
```
Sinister merchant theme, 90 BPM. Dissonant carnival-esque music box melodies. Chains rattling rhythm section, whisper-like vocal pads. Dark jazz influences, corrupted circus atmosphere. Predatory curiosity and twisted elegance. Glitchy dimensional sound effects.
```

**使用シーン**: ゼクの店、取引の提案、影からの囁き

---

#### **Zek_Hideout (次元のゴミ捨て場)**
```
Eerie collection ambient, 60 BPM. Unsettling music box fragments, distant screams (very subtle). Glass case resonance, frozen time sounds. Museum of despair atmosphere. Creaking metal, dimensional static. Horrifying yet fascinating.
```

**使用シーン**: ゼクの隠れ家、コレクションの展示、英雄の剥製

---

### **8. ストーリー・感情BGM**

#### **Emotional_Sorrow (哀しみの静寂)**
```
Quiet mourning theme, 60 BPM. Melancholic solo piano or strings. Distant wind ambience, subtle ice melting sounds. Sorrowful but gentle. Post-battle grief and reflection. Minimal orchestration for emotional space.
```

**使用シーン**: カイン戦後、英雄の残党戦後、喪失と悲しみ

---

#### **Emotional_Sacred_Triumph (理を拒む勝利)**
```
Sacred victory theme, 100 BPM. Majestic orchestral with warm undertones. Hopeful brass fanfares, uplifting strings. Cathedral-like reverb. Earned victory and redemption atmosphere. Not celebratory but sacred relief.
```

**使用シーン**: バルガスを救った後、システムを破壊した瞬間、真の勝利

---

#### **Emotional_Warmth (仲間との絆)**
```
Warm companionship theme, 95 BPM. Gentle orchestral with crackling fire sounds. Acoustic instruments (guitar, woodwinds), friendly percussion. Campfire intimacy, found family atmosphere. Melancholic yet hopeful.
```

**使用シーン**: バルガスとの深夜の会話、仲間との語らい、絆の確認

---

#### **Emotional_Resolve (静かな決意)**
```
Quiet determination theme, 75 BPM. Somber strings building to resolute brass. Footsteps on stone, sword being drawn. Inner strength and resolve. Not loud but unshakeable. Building emotional intensity.
```

**使用シーン**: 重大な決断、戦いへの覚悟、決戦前夜

---

#### **Emotional_Hope (新たな始まり)**
```
Hopeful new beginning theme, 90 BPM. Gentle orchestral with birdsong, wind through grass. Breaking chains SFX transitioning to nature sounds. Sunrise atmosphere, emotional release. Freedom and possibility.
```

**使用シーン**: アリーナ崩壊後、地上への帰還、新しい旅の始まり

---

### **9. 最終章専用BGM**

#### **Final_PreBattle_Calm (決戦前夜)**
```
Quiet pre-battle vigil, 65 BPM. Somber orchestral minimalism. Magic circuit ambient hum, distant fire crackling. Reflective strings, determined brass undertones. Last night before final battle - resolve and friendship. Heavy with fate.
```

**使用シーン**: 最終章 第1幕、ゴミ捨て場での静かな誓い

---

#### **Final_Astaroth_Throne (虚空の王座)**
```
Silent throne room tension, 40 BPM. Extremely sparse orchestration with massive heartbeat bass. Cathedral-like reverberations, dimensional void sounds. Absolute silence punctuated by giant heartbeats. Facing a god - ultimate gravity and isolation.
```

**使用シーン**: 最終章 第2幕、アスタロト降臨、王座の間

---

#### **Final_Liberation (解放の光)**
```
Freedom and hope theme, 105 BPM. Triumphant yet gentle orchestral. Breaking glass/chains SFX transitioning to birds chirping, wind through grass. Emergence into real world after dimensional prison. Emotional release and new beginnings. Warm sunrise atmosphere.
```

**使用シーン**: 最終章 第6幕、次元の壁が崩壊、地上への階段

---

#### **Ending_Celebration (仲間との祝杯)**
```
Bright celebration theme, 120 BPM. Upbeat fantasy tavern music. Acoustic guitars, flutes, hand drums. Joyful and friendly atmosphere. Companions together in freedom. Light-hearted ending credits music.
```

**使用シーン**: エンディング、スタッフロール後、仲間との祝杯

---

### **10. 環境・演出用BGM**

#### **Crafting_Ambient (工房の静寂)**
```
Quiet crafting/work theme, 70 BPM. Minimal ambient music for concentration. Gentle chimes, soft pads, workshop sounds (hammer on anvil, gentle). Meditative and focused atmosphere. Non-intrusive background.
```

**使用シーン**: 死の共鳴瓶作成、虚空の心臓作成、クラフト作業

---

#### **Mystical_Ritual (神秘の儀式)**
```
Magical ritual theme, 80 BPM. Mystical ambient with bell tones and choral pads. Arcane humming, crystalline sound effects. Magic circle activation atmosphere. Sacred and otherworldly.
```

**使用シーン**: リリィの実験、魔法の儀式、魔力の測定

---

#### **Fanfare_Audience (華やかなファンファーレ)**
```
Grandiose arena fanfare, 110 BPM. Triumphant brass fanfares, military drums. Circus-like showmanship, crowd cheering SFX. Grand spectacle atmosphere. Ironic celebration of bloodsport.
```

**使用シーン**: Rank D以降の戦闘開始時、観客の歓声、アナウンス

---

## **音楽的統一感のための指針**

### **共通楽器パレット**
- **弦楽器**: オーケストラストリングス（ヴァイオリン、チェロ）
- **金管楽器**: ブラス（トランペット、ホルン、トロンボーン）
- **打楽器**: タイコ、戦闘ドラム、チェーン音
- **特殊音色**: ガラスハーモニカ、ミュージックボックス、教会オルガン
- **環境音**: 鎖、金属、氷、炎、心音

### **音楽的モチーフ**
- **心音リズム**: 72 BPM（人間の心拍数）を基準とした緊張感
- **鎖の音**: アリーナの拘束を象徴
- **不協和音**: 次元の歪み、ゼクの存在
- **教会オルガン**: アスタロトの神性
- **紫煙テクスチャ**: リリィのサキュバス性

### **感情の音楽的表現**
- **絶望**: 低音ドローン、減7和音
- **希望**: 長調への転調、上昇するメロディ
- **緊張**: 不規則なリズム、半音階
- **親密**: 少数楽器、柔らかい音色
- **戦闘**: 4/4拍子、アクセント強調

---

## **技術仕様**

### **推奨フォーマット**
- **ファイル形式**: OGG Vorbis（ループ対応）または MP3
- **サンプルレート**: 48kHz
- **ビットレート**: 192kbps以上
- **ループポイント**: 必ず設定（戦闘BGMは特に重要）

### **ダイナミクス**
- **ロビー/環境**: -12dB ~ -18dB（控えめ）
- **戦闘**: -6dB ~ -12dB（中程度）
- **ボス戦**: -3dB ~ -6dB（大音量）
- **感情シーン**: -12dB ~ -15dB（静か）

### **ループ設計**
- **戦闘BGM**: 1-2分ループ、シームレス必須
- **環境BGM**: 2-3分ループ、自然な継ぎ目
- **ボスBGM**: 2-4分、イントロ+ループ構造
- **ストーリーBGM**: ループ不要の場合もあり

---

**作成日**: 2026-01-04
**更新日**: 2026-01-05
**総BGM数**: 39曲（実装済み34曲、未実装5曲）
**実装進捗率**: 87.2%
