# -*- coding: utf-8 -*-
"""
アリーナマスター（バルガス）のメインダイアログ

高レベルAPIを使用した宣言的定義
"""

import importlib

from arena.builders import ChoiceReaction, DramaBuilder
from arena.data import (
    QUEST_DONE_PREFIX,
    Actors,
    BattleStageDefinition,
    DramaNames,
    Keys,
    MenuItem,
    QuestBattleFlags,
    QuestEntry,
    QuestIds,
    QuestInfoDefinition,
    QuestStartDefinition,
    Rank,
    RankDefinition,
    RankUpTrialFlags,
    SessionKeys,
)
from arena.scenarios.rank_up.rank_a import add_rank_up_A_result_steps
from arena.scenarios.rank_up.rank_b import add_rank_up_B_result_steps
from arena.scenarios.rank_up.rank_c import add_rank_up_C_result_steps
from arena.scenarios.rank_up.rank_d import add_rank_up_D_result_steps
from arena.scenarios.rank_up.rank_e import add_rank_up_E_result_steps
from arena.scenarios.rank_up.rank_f import add_rank_up_F_result_steps

# ランクアップ結果ステップ関数のインポート
from arena.scenarios.rank_up.rank_g import add_rank_up_G_result_steps

# モジュール名が数字で始まるためimportlibを使用
_upper_existence_module = importlib.import_module("arena.scenarios.07_upper_existence")
add_upper_existence_result_steps = (
    _upper_existence_module.add_upper_existence_result_steps
)

_last_battle_module = importlib.import_module("arena.scenarios.18_last_battle")
add_last_battle_result_steps = _last_battle_module.add_last_battle_result_steps

_vs_balgas_module = importlib.import_module("arena.scenarios.15_vs_balgas")
add_vs_balgas_result_steps = _vs_balgas_module.add_vs_balgas_result_steps

_balgas_training_module = importlib.import_module("arena.scenarios.09_balgas_training")
add_balgas_training_result_steps = (
    _balgas_training_module.add_balgas_training_result_steps
)


# ============================================================================
# データ定義
# ============================================================================

# ランクアップ試験定義
RANK_DEFINITIONS = [
    RankDefinition(
        rank="g",
        quest_id=QuestIds.RANK_UP_G,
        drama_name=DramaNames.RANK_UP_G,
        confirm_msg="ほう…『屑肉の洗礼』を受けるつもりか？死んでも文句は言えんぞ。",
        confirm_msg_en="Hoh... You're takin' on the 'Meat Grinder Trial'? Don't come cryin' if you die.",
        confirm_msg_cn="哦……你小子要挑战『碎肉洗礼』？死了可别怪老子。",
        confirm_button="問題ない",
        confirm_button_en="No problem",
        confirm_button_cn="没问题",
        trial_flag_value=1,
        quest_flag_value=11,
        result_steps_func=add_rank_up_G_result_steps,
    ),
    RankDefinition(
        rank="f",
        quest_id=QuestIds.RANK_UP_F,
        drama_name=DramaNames.RANK_UP_F,
        confirm_msg="『凍土の魔犬』との戦いだな。覚悟はいいか？",
        confirm_msg_en="The 'Frost Hound' fight, eh? You ready for this?",
        confirm_msg_cn="『冻土魔犬』之战吗。准备好了？",
        confirm_button="いくぞ",
        confirm_button_en="Let's go",
        confirm_button_cn="上吧",
        trial_flag_value=2,
        quest_flag_value=12,
        result_steps_func=add_rank_up_F_result_steps,
    ),
    RankDefinition(
        rank="e",
        quest_id=QuestIds.RANK_UP_E,
        drama_name=DramaNames.RANK_UP_E,
        confirm_msg="『カイン亡霊戦』だな。あいつは……優秀な剣士だった。覚悟はいいか？",
        confirm_msg_en="The 'Cain's Ghost' trial. He was... the strongest gladiator I ever knew. You ready?",
        confirm_msg_cn="『凯恩亡灵战』吗。那家伙……是老子认识的最强角斗士。准备好了？",
        confirm_button="挑む",
        confirm_button_en="I'll face him",
        confirm_button_cn="我来挑战",
        trial_flag_value=3,
        quest_flag_value=13,
        result_steps_func=add_rank_up_E_result_steps,
    ),
    RankDefinition(
        rank="d",
        quest_id=QuestIds.RANK_UP_D,
        drama_name=DramaNames.RANK_UP_D,
        confirm_msg="『銅貨稼ぎの洗礼』だな。観客のヤジが降ってくる。避けながら戦えるか？",
        confirm_msg_en="The 'Copper-Earner's Trial'. The crowd'll rain junk on ya. Can you fight while dodgin'?",
        confirm_msg_cn="『铜币赚手的洗礼』吗。观众会朝你扔东西。你能边躲边战斗吗？",
        confirm_button="やってみる",
        confirm_button_en="I'll try",
        confirm_button_cn="试试看",
        trial_flag_value=4,
        quest_flag_value=14,
        result_steps_func=add_rank_up_D_result_steps,
    ),
    RankDefinition(
        rank="c",
        quest_id=QuestIds.RANK_UP_C,
        drama_name=DramaNames.RANK_UP_C,
        confirm_msg="『朱砂食い』への試練だな……俺のかつての仲間たちと戦ってもらう。",
        confirm_msg_en="The 'Cinnabar Devourer' trial... You'll be fightin' my old comrades.",
        confirm_msg_cn="『噬朱砂者』的试炼吗……你要和老子以前的伙伴们战斗。",
        confirm_button="分かった",
        confirm_button_en="Understood",
        confirm_button_cn="明白了",
        trial_flag_value=5,
        quest_flag_value=15,
        result_steps_func=add_rank_up_C_result_steps,
    ),
    RankDefinition(
        rank="b",
        quest_id=QuestIds.RANK_UP_B,
        drama_name=DramaNames.RANK_UP_B,
        confirm_msg="『虚無の処刑人』……ヌルとの戦いだ。あいつは、虚空そのものだ。覚悟はいいか？",
        confirm_msg_en="The 'Executioner of Void'... Null. That thing IS the void itself. You ready?",
        confirm_msg_cn="『虚无的处刑人』……与Nul的战斗。那家伙就是虚空本身。准备好了？",
        confirm_button="挑む",
        confirm_button_en="I'll face it",
        confirm_button_cn="我来挑战",
        trial_flag_value=6,
        quest_flag_value=16,
        result_steps_func=add_rank_up_B_result_steps,
    ),
    RankDefinition(
        rank="a",
        quest_id=QuestIds.RANK_UP_A,
        drama_name=DramaNames.RANK_UP_A,
        confirm_msg="『影との戦い』だ。お前自身の影と戦うことになる。覚悟はいいか？",
        confirm_msg_en="The 'Battle with Shadow'. You'll fight your own shadow. Ready for that?",
        confirm_msg_cn="『与影之战』。你要和自己的影子战斗。准备好了？",
        confirm_button="挑む",
        confirm_button_en="I'll face it",
        confirm_button_cn="我来挑战",
        trial_flag_value=7,
        quest_flag_value=17,
        result_steps_func=add_rank_up_A_result_steps,
    ),
]

# 挨拶定義（シンプル化: 全ランク共通）
SIMPLE_GREETING = "おう、闘士よ。今日は何の用だ？"
SIMPLE_GREETING_EN = "Hey, fighter. What brings you here today?"
SIMPLE_GREETING_CN = "哟，角斗士。今天有什么事？"

# バトルステージ定義
BATTLE_STAGES = [
    BattleStageDefinition(
        stage_num=1,
        stage_id="stage_1",
        advice="お前の最初の相手は「森の狼」だ。素早い攻撃には気をつけろ。武器と防具は整えたか？回復アイテムもあると安心だぞ。",
        advice_en="Your first opponent is the 'Forest Wolf'. Watch out for its quick attacks. Got your weapon and armor ready? Bring some healing items too.",
        advice_cn="你的第一个对手是『森之狼』。小心它的快速攻击。武器和防具准备好了吗？带上回复道具会更安心。",
        advice_id="stage1_advice",
        sendoff="よし、行け！生きて戻ってこい...できればな。",
        sendoff_en="Alright, go! Come back alive... if you can.",
        sendoff_cn="好，去吧！活着回来……如果能的话。",
        sendoff_id="sendoff1",
        go_button="準備できた、行く！",
        go_button_en="Ready, let's go!",
        go_button_cn="准备好了，出发！",
        cancel_button="もう少し準備してくる",
        cancel_button_en="I need more time to prepare",
        cancel_button_cn="再准备一下",
        next_stage_flag=2,
    ),
    BattleStageDefinition(
        stage_num=2,
        stage_id="stage_2",
        advice="次の相手は「ケンタウロス」だ。奴の突進は威力があるぞ。",
        advice_en="Next up is the 'Centaur'. That thing's charge hits hard.",
        advice_cn="下一个对手是『半人马』。那家伙的冲锋威力很大。",
        advice_id="stage2_advice",
        sendoff="いい度胸だ。お前ならやれる！",
        sendoff_en="Good guts. You got this!",
        sendoff_cn="有胆量。你能行的！",
        sendoff_id="sendoff2",
        go_button="準備できた！",
        go_button_en="I'm ready!",
        go_button_cn="准备好了！",
        cancel_button="待ってくれ",
        cancel_button_en="Hold on",
        cancel_button_cn="等一下",
        next_stage_flag=3,
    ),
    BattleStageDefinition(
        stage_num=3,
        stage_id="stage_3",
        advice="ここからが本番だ。「ミノタウロス」...奴は俺も手こずった相手だ。力任せに攻めるな。奴の隙を狙え。",
        advice_en="Now the real fight begins. The 'Minotaur'... even I had trouble with that beast. Don't go in swinging wild. Wait for its openings.",
        advice_cn="从这里开始才是真正的战斗。『牛头人』……连老子都曾吃过苦头。别蛮干，瞄准破绽。",
        advice_id="stage3_advice",
        sendoff="...無茶するなよ。お前はもうただの新人じゃない。",
        sendoff_en="...Don't do anything stupid. You ain't just fresh meat anymore.",
        sendoff_cn="……别乱来。你已经不是新手了。",
        sendoff_id="sendoff3",
        go_button="挑む！",
        go_button_en="I'll take it on!",
        go_button_cn="我来挑战！",
        cancel_button="...もう少し鍛えてくる",
        cancel_button_en="...I need more training",
        cancel_button_cn="……再锻炼一下",
        next_stage_flag=4,
    ),
    BattleStageDefinition(
        stage_num=4,
        stage_id="stage_4",
        advice="よくぞここまで来た。最後の相手は...グランドマスターだ。覚悟はいいか？あれは...俺でも勝てるかわからん相手だ。",
        advice_en="You've come far. Your final opponent is... the Grand Master. You ready? That one... even I ain't sure I could beat it.",
        advice_cn="你能走到这里真不容易。最后的对手是……大师。准备好了吗？那个……连老子都不知道能不能赢。",
        advice_id="champion_advice",
        sendoff="...見届けてやる。行って来い、闘士よ。",
        sendoff_en="...I'll be watching. Go, fighter.",
        sendoff_cn="……老子会看着的。去吧，角斗士。",
        sendoff_id="sendoff_champ",
        go_button="俺は負けない",
        go_button_en="I won't lose",
        go_button_cn="我不会输",
        cancel_button="...考え直す",
        cancel_button_en="...Let me reconsider",
        cancel_button_cn="……让我再想想",
        next_stage_flag=None,  # 最後のステージ
    ),
]

# クエスト情報定義（ゼク・リリィ関連 - 情報提供のみ）
QUEST_INFOS = [
    # ゼク関連
    QuestInfoDefinition(
        "quest_zek_intro",
        "quest_zek_info",
        [
            "そういえば、例の商人が来てるぞ。『ゼク』って名乗る怪しい野郎だ。",
            "ロビーの隅にいるはずだ。興味があるなら話しかけてみろ。",
        ],
        messages_en=[
            "Oh yeah, that merchant showed up. Shady guy calls himself 'Zek'.",
            "Should be lurkin' in the corner of the lobby. Go talk to him if you're curious.",
        ],
        messages_cn=[
            "对了，那个商人来了。自称『泽克』的可疑家伙。",
            "应该在大厅角落里。有兴趣的话去跟他聊聊。",
        ],
    ),
    QuestInfoDefinition(
        "quest_zek_steal_bottle",
        "quest_zek_bottle_info",
        [
            "ゼクの野郎が何やら企んでやがる。あいつに話しかけてみろ。",
            "……俺は関わらねえが、お前の判断だ。",
        ],
        messages_en=[
            "That Zek bastard's scheming something. Go talk to him.",
            "...I ain't gettin' involved, but it's your call.",
        ],
        messages_cn=[
            "那个泽克家伙在图谋什么。去跟他谈谈。",
            "……老子不掺和，但你自己决定。",
        ],
    ),
    QuestInfoDefinition(
        "quest_zek_steal_soulgem",
        "quest_zek_soulgem_info",
        [
            "ゼクがカインの魂について何か言いたいことがあるらしい。",
            "あいつのところへ行ってみろ。……慎重にな。",
        ],
        messages_en=[
            "Zek's got somethin' to say about Cain's soul.",
            "Go see him. ...Watch yourself.",
        ],
        messages_cn=[
            "泽克好像有关于凯恩灵魂的事要说。",
            "去找他吧。……小心点。",
        ],
    ),
    # リリィ関連
    QuestInfoDefinition(
        "quest_lily_exp",
        "quest_lily_info",
        [
            "リリィが何やら困ってるらしいぜ。『死の共鳴瓶』とかいう怪しげなアイテムを作りたいとか。",
            "あいつのところへ行って話を聞いてやれ。",
        ],
        messages_en=[
            "Lily's got some trouble, apparently. Wants to make some weird thing called a 'Death Resonance Bottle'.",
            "Go hear her out.",
        ],
        messages_cn=[
            "莉莉好像遇到了什么麻烦。说是想做个叫『死亡共鸣瓶』的奇怪东西。",
            "去听听她怎么说。",
        ],
    ),
    QuestInfoDefinition(
        "quest_lily_private",
        "quest_lily_priv_info",
        [
            "リリィが『自分の過去』について話したいらしい。……珍しいな。",
            "興味があるならあいつに話しかけてやれ。",
        ],
        messages_en=[
            "Lily wants to talk about 'her past'. ...That's rare.",
            "Go talk to her if you're interested.",
        ],
        messages_cn=[
            "莉莉好像想谈谈『自己的过去』。……真稀奇。",
            "有兴趣的话去跟她聊聊。",
        ],
    ),
    QuestInfoDefinition(
        "quest_makuma",
        "quest_makuma_info",
        ["リリィがお前を探していたぞ。"],
        messages_en=["Lily was looking for you."],
        messages_cn=["莉莉在找你。"],
    ),
    QuestInfoDefinition(
        "quest_lily_real_name",
        "quest_lily_name_info",
        ["リリィがお前を探していたぞ。"],
        messages_en=["Lily was looking for you."],
        messages_cn=["莉莉在找你。"],
    ),
    # 自動発動系
    QuestInfoDefinition(
        "quest_makuma2",
        "quest_makuma2_info",
        ["リリィがお前を探していたぞ。"],
        messages_en=["Lily was looking for you."],
        messages_cn=["莉莉在找你。"],
    ),
    QuestInfoDefinition(
        "quest_vs_astaroth",
        "quest_astaroth_info",
        ["……いよいよだな。アスタロトとの戦いが近い。", "覚悟しておけ。"],
        messages_en=[
            "...It's time. The battle with Astaroth is near.",
            "Steel yourself.",
        ],
        messages_cn=["……终于要来了。与阿斯塔罗特的战斗近在眼前。", "做好觉悟。"],
    ),
    QuestInfoDefinition(
        "quest_last_battle",
        "quest_last_info",
        ["……これが最後の戦いだ。", "お前は何のために戦う？　答えを見つけておけ。"],
        messages_en=[
            "...This is the final battle.",
            "What are you fighting for? Find your answer.",
        ],
        messages_cn=["……这是最后的战斗了。", "你为了什么而战？找到你的答案。"],
    ),
    # ランクアップ情報
    QuestInfoDefinition(
        "quest_rank_up_g",
        "quest_rank_g_info",
        [
            "【昇格試合】ランクG『屑肉の洗礼』が受けられるぜ。「昇格試合を受けたい」を選んでくれ。"
        ],
        messages_en=[
            "[Rank-Up Match] Rank G 'Meat Grinder Trial' is available. Select 'Take rank-up match'."
        ],
        messages_cn=["【晋级赛】G等级『碎肉洗礼』可以挑战了。选择「参加晋级赛」吧。"],
    ),
    QuestInfoDefinition(
        "quest_rank_up_f",
        "quest_rank_f_info",
        [
            "【昇格試合】ランクF『凍土の魔犬』が受けられるぜ。「昇格試合を受けたい」を選んでくれ。"
        ],
        messages_en=[
            "[Rank-Up Match] Rank F 'Frost Hound' is available. Select 'Take rank-up match'."
        ],
        messages_cn=["【晋级赛】F等级『冻土魔犬』可以挑战了。选择「参加晋级赛」吧。"],
    ),
    QuestInfoDefinition(
        "quest_rank_up_e",
        "quest_rank_e_info",
        [
            "【昇格試合】ランクE『カインの亡霊』が受けられるぜ。「昇格試合を受けたい」を選んでくれ。"
        ],
        messages_en=[
            "[Rank-Up Match] Rank E 'Cain's Ghost' is available. Select 'Take rank-up match'."
        ],
        messages_cn=["【晋级赛】E等级『凯恩的亡灵』可以挑战了。选择「参加晋级赛」吧。"],
    ),
    QuestInfoDefinition(
        "quest_rank_up_d",
        "quest_rank_d_info",
        [
            "【昇格試合】ランクD『銅貨稼ぎの洗礼』が受けられるぜ。「昇格試合を受けたい」を選んでくれ。"
        ],
        messages_en=[
            "[Rank-Up Match] Rank D 'Copper-Earner's Trial' is available. Select 'Take rank-up match'."
        ],
        messages_cn=[
            "【晋级赛】D等级『铜币赚手的洗礼』可以挑战了。选择「参加晋级赛」吧。"
        ],
    ),
    QuestInfoDefinition(
        "quest_rank_up_c",
        "quest_rank_c_info",
        [
            "【昇格試合】ランクC『朱砂食い』が受けられるぜ。「昇格試合を受けたい」を選んでくれ。"
        ],
        messages_en=[
            "[Rank-Up Match] Rank C 'Cinnabar Devourer' is available. Select 'Take rank-up match'."
        ],
        messages_cn=["【晋级赛】C等级『噬朱砂者』可以挑战了。选择「参加晋级赛」吧。"],
    ),
    QuestInfoDefinition(
        "quest_rank_up_b",
        "quest_rank_b_info",
        [
            "【昇格試合】ランクB『虚無の処刑人』が受けられるぜ。「昇格試合を受けたい」を選んでくれ。"
        ],
        messages_en=[
            "[Rank-Up Match] Rank B 'Executioner of Void' is available. Select 'Take rank-up match'."
        ],
        messages_cn=[
            "【晋级赛】B等级『虚无的处刑人』可以挑战了。选择「参加晋级赛」吧。"
        ],
    ),
    QuestInfoDefinition(
        "quest_rank_up_a",
        "quest_rank_a_info",
        [
            "【昇格試合】ランクA『影との戦い』が受けられるぜ。「昇格試合を受けたい」を選んでくれ。"
        ],
        messages_en=[
            "[Rank-Up Match] Rank A 'Battle with Shadow' is available. Select 'Take rank-up match'."
        ],
        messages_cn=["【晋级赛】A等级『与影之战』可以挑战了。选择「参加晋级赛」吧。"],
    ),
]

# バルガスから直接開始可能なクエスト
QUEST_STARTS = [
    QuestStartDefinition(
        info_step="quest_upper_existence",
        start_step="start_upper_existence",
        info_messages=[
            "……お前には『観客』について教えたいと思ってな。",
            "聞く覚悟はあるか？",
        ],
        info_messages_en=[
            "...There's somethin' I want to tell you about the 'Spectators'.",
            "You ready to hear it?",
        ],
        info_messages_cn=[
            "……老子想跟你说说『观众』的事。",
            "你准备好听了吗？",
        ],
        info_id_prefix="quest_upper_info",
        accept_button="聞く",
        accept_button_en="Tell me",
        accept_button_cn="说吧",
        accept_id="c_accept_upper",
        decline_button="今はいい",
        decline_button_en="Not now",
        decline_button_cn="现在不用了",
        decline_id="c_decline_upper",
        start_message="……いいだろう。座れ。",
        start_message_en="...Alright. Sit down.",
        start_message_cn="……好吧。坐下。",
        drama_name=DramaNames.UPPER_EXISTENCE,
    ),
    QuestStartDefinition(
        info_step="quest_balgas_training",
        start_step="start_balgas_training",
        info_messages=[
            "……おい。俺が直接、お前を鍛えてやろうと思ってる。",
            "死にたくなければ付いてこい。どうだ？",
        ],
        info_messages_en=[
            "...Hey. I'm thinkin' of training you myself.",
            "Follow me if you don't wanna die. Whaddya say?",
        ],
        info_messages_cn=[
            "……喂。老子打算亲自训练你。",
            "不想死就跟上来。怎么样？",
        ],
        info_id_prefix="quest_balgas_info",
        accept_button="ついていく",
        accept_button_en="I'll follow",
        accept_button_cn="跟你走",
        accept_id="c_accept_balgas",
        decline_button="今はやめておく",
        decline_button_en="Not right now",
        decline_button_cn="现在算了",
        decline_id="c_decline_balgas",
        start_message="よし、来い！",
        start_message_en="Alright, let's go!",
        start_message_cn="好，来吧！",
        drama_name=DramaNames.BALGAS_TRAINING,
    ),
    QuestStartDefinition(
        info_step="quest_vs_balgas",
        start_step="start_vs_balgas",
        info_messages=[
            "……おい。俺と本気で戦う気はあるか？",
            "これは試験じゃねえ。俺の『決着』だ。",
        ],
        info_messages_en=[
            "...Hey. You wanna fight me for real?",
            "This ain't a test. This is MY 'settling of scores'.",
        ],
        info_messages_cn=[
            "……喂。你想和老子真正一战吗？",
            "这不是考试。这是老子的『了断』。",
        ],
        info_id_prefix="quest_vs_balgas_info",
        accept_button="受けて立つ",
        accept_button_en="Bring it on",
        accept_button_cn="奉陪到底",
        accept_id="c_accept_vs_balgas",
        decline_button="今は遠慮する",
        decline_button_en="Not this time",
        decline_button_cn="这次就算了",
        decline_id="c_decline_vs_balgas",
        start_message="……覚悟はいいな。",
        start_message_en="...You ready for this.",
        start_message_cn="……你准备好了。",
        drama_name=DramaNames.VS_BALGAS,
    ),
]

# クエストディスパッチャー用エントリ
AVAILABLE_QUESTS = [
    # ストーリー系（優先）
    QuestEntry(QuestIds.ZEK_INTRO, 21, "quest_zek_intro"),
    QuestEntry(QuestIds.LILY_EXPERIMENT, 22, "quest_lily_exp"),
    QuestEntry(QuestIds.ZEK_STEAL_BOTTLE, 23, "quest_zek_steal_bottle"),
    QuestEntry(QuestIds.ZEK_STEAL_SOULGEM, 24, "quest_zek_steal_soulgem"),
    QuestEntry(QuestIds.UPPER_EXISTENCE, 25, "quest_upper_existence"),
    QuestEntry(QuestIds.LILY_PRIVATE, 26, "quest_lily_private"),
    QuestEntry(QuestIds.BALGAS_TRAINING, 27, "quest_balgas_training"),
    QuestEntry(QuestIds.MAKUMA, 28, "quest_makuma"),
    QuestEntry(QuestIds.MAKUMA2, 29, "quest_makuma2"),
    QuestEntry(QuestIds.RANK_UP_S, 30, "quest_vs_balgas"),
    QuestEntry(QuestIds.LILY_REAL_NAME, 31, "quest_lily_real_name"),
    QuestEntry(QuestIds.VS_ASTAROTH, 32, "quest_vs_astaroth"),
    QuestEntry(QuestIds.LAST_BATTLE, 33, "quest_last_battle"),
    # ランクアップ系
    QuestEntry(QuestIds.RANK_UP_G, 11, "quest_rank_up_g"),
    QuestEntry(QuestIds.RANK_UP_F, 12, "quest_rank_up_f"),
    QuestEntry(QuestIds.RANK_UP_E, 13, "quest_rank_up_e"),
    QuestEntry(QuestIds.RANK_UP_D, 14, "quest_rank_up_d"),
    QuestEntry(QuestIds.RANK_UP_C, 15, "quest_rank_up_c"),
    QuestEntry(QuestIds.RANK_UP_B, 16, "quest_rank_up_b"),
    QuestEntry(QuestIds.RANK_UP_A, 17, "quest_rank_up_a"),
]

# 昇格試合用クエストエントリ（rank_up_checkで使用）
RANK_UP_QUESTS = [
    QuestEntry(QuestIds.RANK_UP_G, 11, "start_rank_g"),
    QuestEntry(QuestIds.RANK_UP_F, 12, "start_rank_f"),
    QuestEntry(QuestIds.RANK_UP_E, 13, "start_rank_e"),
    QuestEntry(QuestIds.RANK_UP_D, 14, "start_rank_d"),
    QuestEntry(QuestIds.RANK_UP_C, 15, "start_rank_c"),
    QuestEntry(QuestIds.RANK_UP_B, 16, "start_rank_b"),
    QuestEntry(QuestIds.RANK_UP_A, 17, "start_rank_a"),
]


# ============================================================================
# ドラマ定義
# ============================================================================


def define_arena_master_drama(builder: DramaBuilder):
    """
    アリーナマスターのドラマを定義
    高レベルAPIを使用した宣言的記述
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    vargus = builder.register_actor(Actors.BALGAS, "バルガス", "Vargus")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily")
    astaroth = builder.register_actor(Actors.ASTAROTH, "アスタロト", "Astaroth")

    # 基本ラベル定義
    main = builder.label("main")
    victory_comment = builder.label("victory_comment")
    defeat_comment = builder.label("defeat_comment")
    registered = builder.label("registered")
    pre_registered = builder.label("pre_registered")
    registered_choices = builder.label("registered_choices")
    end = builder.label("end")

    # 機能ラベル
    battle_prep = builder.label("battle_prep")
    rank_check = builder.label("rank_check")
    rank_up_check = builder.label("rank_up_check")
    rank_up_not_ready = builder.label("rank_up_not_ready")
    check_available_quests = builder.label("check_available_quests")
    quest_none = builder.label("quest_none")

    # ========================================
    # 共通選択肢ヘルパー
    # ========================================
    _choice_counter = [0]  # リストでラップして非ローカル変数として使用

    def add_choices(b):
        """共通の選択肢を追加"""
        _choice_counter[0] += 1
        suffix = f"_{_choice_counter[0]}"
        b.add_menu(
            [
                MenuItem(
                    "対戦を組む",
                    battle_prep,
                    f"c_arrange{suffix}",
                    text_en="Arrange a match",
                    text_cn="安排对战",
                ),
                MenuItem(
                    "ランクを確認したい",
                    rank_check,
                    f"c_rank_check{suffix}",
                    text_en="Check my rank",
                    text_cn="确认等级",
                ),
                MenuItem(
                    "昇格試合を受けたい",
                    rank_up_check,
                    f"c_rank_up{suffix}",
                    text_en="Take rank-up match",
                    text_cn="参加晋级赛",
                ),
                MenuItem(
                    "利用可能なクエストを確認",
                    check_available_quests,
                    f"c_check_quests{suffix}",
                    text_en="Check available quests",
                    text_cn="确认可用任务",
                ),
                MenuItem(
                    "また今度",
                    end,
                    f"c_maybe_later{suffix}",
                    text_en="Maybe later",
                    text_cn="下次再说",
                ),
            ],
            cancel=end,
        )

    # ========================================
    # メインエントリーポイント
    # ========================================
    # 未登録プレイヤー用のオープニング開始
    opening_start = builder.label("opening_start")

    builder.step(main).branch_if(
        SessionKeys.IS_RANK_UP_RESULT, "==", 1, "rank_up_result_check"
    ).branch_if(
        SessionKeys.IS_QUEST_BATTLE_RESULT, "==", 1, "quest_battle_result_check"
    ).branch_if(SessionKeys.ARENA_RESULT, "==", 1, victory_comment).branch_if(
        SessionKeys.ARENA_RESULT, "==", 2, defeat_comment
    ).branch_if(SessionKeys.GLADIATOR, "==", 1, pre_registered).jump(opening_start)

    # 未登録プレイヤー: オープニングドラマを開始
    builder.step(opening_start)._start_drama(DramaNames.OPENING).finish()

    # ========================================
    # ランクアップ結果チェック
    # ========================================
    # ランクアップシステムを先に自動生成（ラベルを取得するため）
    rank_labels = builder.build_rank_system(
        RANK_DEFINITIONS,
        actor=vargus,
        fallback_step=registered,
        cancel_step=registered_choices,
        end_step=end,
    )

    rank_up_result_check = builder.label("rank_up_result_check")
    # sukutsu_rank_up_trial: 1=G, 2=F, 3=E, 4=D, 5=C, 6=B, 7=A
    trial_cases = [registered]  # 0: フォールバック
    for rank_def in RANK_DEFINITIONS:
        # build_rank_systemから返されたラベルを使用（同一オブジェクト参照）
        trial_cases.append(rank_labels[f"rank_up_result_{rank_def.rank}"])
    trial_cases.append(registered)  # 末尾フォールバック

    builder.step(rank_up_result_check).set_flag(
        SessionKeys.IS_RANK_UP_RESULT, 0
    ).switch_flag(SessionKeys.RANK_UP_TRIAL, trial_cases)

    # ========================================
    # クエストバトル結果チェック
    # ========================================
    quest_battle_result_check = builder.label("quest_battle_result_check")
    upper_existence_victory = builder.label("upper_existence_victory")
    upper_existence_defeat = builder.label("upper_existence_defeat")
    last_battle_victory = builder.label("last_battle_victory")
    last_battle_defeat = builder.label("last_battle_defeat")
    vs_balgas_victory = builder.label("vs_balgas_victory")
    vs_balgas_defeat = builder.label("vs_balgas_defeat")

    quest_battle_result_upper = builder.label("quest_battle_result_upper_existence")
    quest_battle_result_last = builder.label("quest_battle_result_last_battle")
    quest_battle_result_vs_balgas = builder.label("quest_battle_result_vs_balgas")
    quest_battle_result_balgas_training = builder.label(
        "quest_battle_result_balgas_training"
    )
    balgas_training_victory = builder.label("balgas_training_victory")
    balgas_training_defeat = builder.label("balgas_training_defeat")

    # sukutsu_quest_battle: 0=なし, 1=upper_existence, 2=vs_balgas, 3=last_battle, 4=balgas_training
    builder.step(quest_battle_result_check).set_flag(
        SessionKeys.IS_QUEST_BATTLE_RESULT, 0
    ).switch_flag(
        SessionKeys.QUEST_BATTLE,
        [
            registered,  # 0: なし
            quest_battle_result_upper,  # 1: upper_existence
            quest_battle_result_vs_balgas,  # 2: vs_balgas
            quest_battle_result_last,  # 3: last_battle
            quest_battle_result_balgas_training,  # 4: balgas_training
        ],
    )

    # sukutsu_arena_result: 0=未設定, 1=勝利, 2=敗北
    # 各ステップでsukutsu_quest_battleをクリア（switch_flag後に次回影響しないように）
    builder.step(quest_battle_result_upper).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).switch_flag(
        SessionKeys.ARENA_RESULT,
        [
            registered,  # 0: 未設定
            upper_existence_victory,  # 1: 勝利
            upper_existence_defeat,  # 2: 敗北
        ],
    )

    builder.step(quest_battle_result_last).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).switch_flag(
        SessionKeys.ARENA_RESULT,
        [
            registered,  # 0: 未設定
            last_battle_victory,  # 1: 勝利
            last_battle_defeat,  # 2: 敗北
        ],
    )

    builder.step(quest_battle_result_vs_balgas).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).switch_flag(
        SessionKeys.ARENA_RESULT,
        [
            registered,  # 0: 未設定
            vs_balgas_victory,  # 1: 勝利
            vs_balgas_defeat,  # 2: 敗北
        ],
    )

    builder.step(quest_battle_result_balgas_training).set_flag(
        QuestBattleFlags.FLAG_NAME, QuestBattleFlags.NONE
    ).switch_flag(
        SessionKeys.ARENA_RESULT,
        [
            registered,  # 0: 未設定
            balgas_training_victory,  # 1: 勝利
            balgas_training_defeat,  # 2: 敗北
        ],
    )

    # クエストバトル勝利/敗北ステップ
    add_upper_existence_result_steps(
        builder, upper_existence_victory, upper_existence_defeat, registered_choices
    )
    add_last_battle_result_steps(
        builder, last_battle_victory, last_battle_defeat, registered_choices
    )
    add_vs_balgas_result_steps(
        builder, vs_balgas_victory, vs_balgas_defeat, registered_choices
    )
    add_balgas_training_result_steps(
        builder, balgas_training_victory, balgas_training_defeat, registered_choices
    )

    # ========================================
    # 通常戦闘結果
    # ========================================
    b = (
        builder.step(victory_comment)
        .set_flag(SessionKeys.ARENA_RESULT, 0)
        .say(
            "vic_msg",
            "やるじゃねえか。だが調子に乗るなよ。",
            "Hah! Not bad. But don't get cocky.",
            "哈！不错嘛。但别得意忘形。",
            actor=vargus,
        )
    )
    add_choices(b)

    b = (
        builder.step(defeat_comment)
        .set_flag(SessionKeys.ARENA_RESULT, 0)
        .say(
            "def_msg",
            "無様だな。出直してこい。",
            "Pathetic. Come back when you're ready.",
            "真丢人。回去重新来过吧。",
            actor=vargus,
        )
    )
    add_choices(b)

    # ========================================
    # ランク確認
    # ========================================
    b = (
        builder.step(rank_check)
        .show_rank_info_log()
        .say(
            "rank_info",
            "現在のステータスをログに表示したぜ。確認しな。",
            "Your current status is in the log. Check it.",
            "你现在的状态已经显示在日志里了。去看看吧。",
            actor=vargus,
        )
    )
    add_choices(b)

    # ========================================
    # 昇格試合チェック
    # ========================================
    # GetAvailableQuests()からランクアップクエストを取得し、
    # sukutsu_next_rank_upフラグでディスパッチ
    builder.step(rank_up_check).check_available_quests_for_npc(
        "sukutsu_arena_master"
    ).switch_flag(
        SessionKeys.NEXT_RANK_UP,
        [
            rank_up_not_ready,  # 0: 利用可能なランクアップなし
            rank_labels["start_rank_g"],  # 1: Gランクアップ
            rank_labels["start_rank_f"],  # 2: Fランクアップ
            rank_labels["start_rank_e"],  # 3: Eランクアップ
            rank_labels["start_rank_d"],  # 4: Dランクアップ
            rank_labels["start_rank_c"],  # 5: Cランクアップ
            rank_labels["start_rank_b"],  # 6: Bランクアップ
            rank_labels["start_rank_a"],  # 7: Aランクアップ
        ],
        fallback=rank_up_not_ready,
    )

    b = builder.step(rank_up_not_ready).say(
        "rank_up_error",
        "まだお前には早い。条件を満たしていないか、すでに昇格済みだ。",
        "Too early for you, kid. Either you ain't qualified or you're already ranked up.",
        "对你来说还太早了。要么不够资格，要么已经晋级过了。",
        actor=vargus,
    )
    add_choices(b)

    # ========================================
    # 登録済みプレイヤーフロー
    # ========================================
    # エピローグ後ラベル
    post_game = builder.label("post_game")
    post_game_choices = builder.label("post_game_choices")
    quest_done_last_battle = f"{QUEST_DONE_PREFIX}{QuestIds.LAST_BATTLE}"

    # エピローグ完了後は専用フローへ
    builder.step(pre_registered).branch_if(
        quest_done_last_battle, "==", 1, post_game
    ).jump(registered)

    # ========================================
    # 挨拶（シンプル化）
    # ========================================
    b = (
        builder.step(registered)
        .check_available_quests_for_npc("sukutsu_arena_master")
        .say(
            "greet",
            SIMPLE_GREETING,
            SIMPLE_GREETING_EN,
            SIMPLE_GREETING_CN,
            actor=vargus,
        )
    )
    add_choices(b)

    # 選択肢のみのステップ（say がないと choice が前のステップの lastTalk に追加されてしまう）
    # エピローグ後は post_game_choices にジャンプ
    b = (
        builder.step(registered_choices)
        .branch_if(quest_done_last_battle, "==", 1, post_game_choices)
        .say(
            "greet_back", "何か用か？", "What do you want?", "有什么事？", actor=vargus
        )
    )
    add_choices(b)

    # ========================================
    # ランダムバトルシステム（1日1回制限・リロール機能付き）
    # ========================================
    random_battle_detail = builder.label("random_battle_detail")
    random_battle_detail_choices = builder.label("random_battle_detail_choices")
    random_battle_start = builder.label("random_battle_start")
    random_battle_reroll = builder.label("random_battle_reroll")
    random_battle_reroll_success = builder.label("random_battle_reroll_success")
    random_battle_reroll_fail = builder.label("random_battle_reroll_fail")
    gimmick_explanation = builder.label("gimmick_explanation")

    # 導入
    builder.step(battle_prep).say(
        "random_battle_intro",
        "今日の対戦相手を用意してある。詳細を確認するか？",
        "I have an opponent ready for you today. Want to see the details?",
        "今天的对手已经准备好了。要确认详情吗？",
        actor=vargus,
    ).choice(
        random_battle_detail,
        "確認する",
        "Check details",
        "确认",
        text_id="c_random_detail",
    ).choice(
        registered_choices,
        "やめておく",
        "Not now",
        "算了",
        text_id="c_random_cancel",
    ).on_cancel(registered_choices)

    # 本日のバトル詳細表示（ローカライズ対応）
    builder.step(random_battle_detail).action(
        "eval",
        param="Elin_SukutsuArena.ArenaManager.ShowRandomBattlePreviewDialog();",
    ).jump(random_battle_detail_choices)

    # 本日のバトル選択肢（詳細表示後、またはギミック説明後に戻る先）
    builder.step(random_battle_detail_choices).say(
        "random_battle_detail",
        "どうする？",
        "What will you do?",
        "怎么办？",
        actor=vargus,
    ).choice(
        random_battle_start,
        "挑戦する",
        "Challenge",
        "挑战",
        text_id="c_random_start",
    ).choice(
        random_battle_reroll,
        "別の相手を希望（コイン10枚）",
        "Different opponent (10 plat)",
        "换个对手（10白金币）",
        text_id="c_random_reroll",
    ).choice(
        gimmick_explanation,
        "特殊ルールについて聞く",
        "Ask about special rules",
        "询问特殊规则",
        text_id="c_gimmick_explain",
    ).choice(
        registered_choices,
        "やめておく",
        "Not now",
        "算了",
        text_id="c_random_back",
    ).on_cancel(registered_choices)

    # 特殊ルール説明
    builder.step(gimmick_explanation).action(
        "eval",
        param="var text = Elin_SukutsuArena.RandomBattle.ArenaGimmickSystem.GetAllGimmickDescriptions(); "
        "Dialog.Ok(text);",
    ).say(
        "gimmick_explained",
        "……他に聞きたいことは？",
        "...Anything else?",
        "……还有什么想问的？",
        actor=vargus,
    ).jump(random_battle_detail_choices)

    # リロール処理
    builder.step(random_battle_reroll).action(
        "eval",
        param="bool success = Elin_SukutsuArena.ArenaManager.RerollTodaysBattle(); "
        f'EClass.player.dialogFlags["{SessionKeys.REROLL_RESULT}"] = success ? 1 : 0;',
    ).branch_if(SessionKeys.REROLL_RESULT, "==", 1, random_battle_reroll_success).jump(
        random_battle_reroll_fail
    )

    # リロール成功 → 詳細表示に戻る
    builder.step(random_battle_reroll_success).say(
        "reroll_success",
        "……新しい対戦相手を用意した。",
        "...I've arranged a new opponent.",
        "……新的对手已经安排好了。",
        actor=vargus,
    ).jump(random_battle_detail)

    # リロール失敗（コイン不足）
    builder.step(random_battle_reroll_fail).say(
        "reroll_fail",
        "プラチナコインが足りないようだな。",
        "You don't have enough platinum coins.",
        "白金币好像不够啊。",
        actor=vargus,
    ).jump(random_battle_detail)

    # 戦闘開始
    builder.step(random_battle_start).say(
        "random_battle_sendoff",
        "いい度胸だ。行ってこい！",
        "Good spirit! Go!",
        "有胆量！去吧！",
        actor=vargus,
    ).action(
        "eval", param="Elin_SukutsuArena.ArenaManager.StartRandomBattle(tg);"
    ).finish()

    # ========================================
    # 固定バトルステージシステム（後方互換・デバッグ用）
    # ========================================
    # fixed_battle_prep = builder.label("fixed_battle_prep")
    # builder.build_battle_stages(
    #     BATTLE_STAGES,
    #     actor=vargus,
    #     entry_step=fixed_battle_prep,
    #     cancel_step=registered_choices,
    #     stage_flag="sukutsu_arena_stage",
    # )

    # ========================================
    # クエストディスパッチャー
    # ========================================
    quest_labels = builder.build_quest_dispatcher(
        AVAILABLE_QUESTS,
        entry_step=check_available_quests,
        fallback_step=quest_none,
        actor=vargus,
        intro_message="どれどれ...",
        intro_message_en="Let me see...",
        intro_message_cn="让老子看看……",
        intro_id="quest_check",
    )

    # クエスト情報ステップ（情報提供のみ）
    builder.build_quest_info_steps(
        QUEST_INFOS,
        actor=vargus,
        return_step=registered_choices,
    )

    # 直接開始可能なクエスト
    builder.build_quest_start_steps(
        QUEST_STARTS,
        actor=vargus,
        cancel_step=registered_choices,
        end_step=end,
    )

    # クエストなし
    builder.step(quest_none).say(
        "no_quest",
        "今は特に依頼はねえな。いまのうちに実力をつけることだ。",
        "No jobs right now. Use this time to get stronger.",
        "现在没什么委托。趁这段时间好好提升实力吧。",
        actor=vargus,
    ).jump(registered_choices)

    # ========================================
    # エピローグ後の会話
    # ========================================
    builder.step(post_game).say(
        "pg_greet",
        "おう、自由の身になったな。……お前に付き合うのも悪くねえ。",
        "Hey, Dragon Slayer. You're free now... Tagging along with you wouldn't be bad.",
        "哟，你自由了啊。……陪你走走也不错。",
        actor=vargus,
    ).jump(post_game_choices)

    # パーティメンバー用選択肢
    # inject_unique(): バニラの_invite, _joinParty, _leaveParty, _buy, _heal等を追加
    builder.step(post_game_choices).inject_unique().choice(
        battle_prep,
        "対戦を組む",
        "Challenge a battle",
        "安排对战",
        text_id="c_pg_battle",
    ).choice(
        end,
        "また話そう",
        "Let's talk again later",
        "下次再聊",
        text_id="c_pg_bye",
    ).on_cancel(end)

    # ========================================
    # 終了
    # ========================================
    builder.step(end).finish()
