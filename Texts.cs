using System.Collections.Generic;

namespace HyperBoost
{
    internal enum AppLanguage { English, Japanese, Korean, Chinese, French, Arabic }

    internal static class Texts
    {
        internal const string Product = "HyperBoost";
        internal static readonly string[] LanguageNames = { "English", "日本語", "한국어", "中文", "Français", "العربية" };

        // Values are indexed by (int)AppLanguage: EN, JA, KO, ZH, FR, AR
        private static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>
        {
            { "sub", new[] { "GAME BOOSTER // SERVICE CONTROL", "ゲームブースター // サービス制御", "게임 부스터 // 서비스 제어", "游戏加速 // 服务控制", "BOOSTEUR DE JEU // CONTRÔLE DES SERVICES", "معزز الألعاب // التحكم بالخدمات" } },
            { "refresh", new[] { "Refresh", "更新", "새로 고침", "刷新", "Actualiser", "تحديث" } },
            { "service", new[] { "Service", "サービス", "서비스", "服务", "Service", "الخدمة" } },
            { "name", new[] { "Service name", "サービス名", "서비스 이름", "服务名称", "Nom du service", "اسم الخدمة" } },
            { "status", new[] { "Status", "状態", "상태", "状态", "Statut", "الحالة" } },
            { "start", new[] { "Start", "開始", "시작", "启动", "Démarrer", "تشغيل" } },
            { "stop", new[] { "Stop", "停止", "중지", "停止", "Arrêter", "إيقاف" } },
            { "startAll", new[] { "Start all", "すべて開始", "모두 시작", "全部启动", "Tout démarrer", "تشغيل الكل" } },
            { "stopAll", new[] { "Stop all", "すべて停止", "모두 중지", "全部停止", "Tout arrêter", "إيقاف الكل" } },
            { "footer", new[] { "Service changes run in the background. Administrator permission is required.", "サービスの変更はバックグラウンドで実行されます。管理者権限が必要です。", "서비스 변경은 백그라운드에서 실행됩니다. 관리자 권한이 필요합니다.", "服务更改在后台运行。需要管理员权限。", "Les changements de services s'exécutent en arrière-plan. Droits administrateur requis.", "تعمل تغييرات الخدمات في الخلفية. مطلوب صلاحيات المسؤول." } },
            { "loading", new[] { "Loading configured services...", "サービス状態を読み込み中...", "서비스 상태를 불러오는 중...", "正在加载服务状态...", "Chargement des services...", "جارٍ تحميل الخدمات..." } },
            { "activeFmt", new[] { "ACTIVE SERVICES  {0} / {1}", "実行中のサービス: {0} / {1}", "실행 중인 서비스: {0} / {1}", "运行中的服务: {0} / {1}", "SERVICES ACTIFS  {0} / {1}", "الخدمات النشطة: {0} / {1}" } },
            { "optimize", new[] { "OPTIMIZATION", "最適化", "최적화", "一键优化", "OPTIMISATION", "التحسين" } },
            { "optimizing", new[] { "OPTIMIZING", "最適化中", "최적화 중", "优化中", "OPTIMISATION", "جارٍ التحسين" } },
            { "stoppingServices", new[] { "STOPPING SERVICES", "サービス停止中", "서비스 중지 중", "正在停止服务", "ARRÊT DES SERVICES", "إيقاف الخدمات" } },
            { "flushingMemory", new[] { "FLUSHING MEMORY", "メモリ解放中", "메모리 비우는 중", "正在清理内存", "LIBÉRATION MÉMOIRE", "تفريغ الذاكرة" } },
            { "measuring", new[] { "MEASURING", "測定中", "메모리 측정 중", "正在测量", "MESURE", "جارٍ القياس" } },
            { "ramFreedFmt", new[] { "{0} RAM FREED", "メモリ {0} 解放", "RAM {0} 해방", "已释放 {0} 内存", "{0} DE RAM LIBÉRÉS", "تم تحرير {0} من الذاكرة" } },
            { "boost", new[] { "PERFORMANCE BOOST", "パフォーマンスブースト", "성능 부스트", "性能加速", "BOOST DE PERFORMANCE", "تعزيز الأداء" } },
            { "boosting", new[] { "BOOSTING", "ブースト中", "부스트 중", "加速中", "BOOST", "جارٍ التعزيز" } },
            { "restore", new[] { "REVERT BOOST", "ブースト解除", "부스트 해제", "撤销加速", "ANNULER LE BOOST", "إلغاء التعزيز" } },
            { "restoring", new[] { "REVERTING", "解除中", "해제 중", "撤销中", "ANNULATION", "جارٍ الإلغاء" } },
            { "failed", new[] { "FAILED", "失敗", "실패", "失败", "ÉCHEC", "فشل" } },
            { "powerPhase", new[] { "POWER PLAN", "電源プラン", "전원 설정", "电源计划", "PLAN D'ALIMENTATION", "خطة الطاقة" } },
            { "gpuPhase", new[] { "GPU MODE", "GPU モード", "GPU 모드", "GPU 模式", "MODE GPU", "وضع GPU" } },
            { "timerPhase", new[] { "TIMER RESOLUTION", "タイマー分解能", "타이머 해상도", "计时器分辨率", "RÉSOLUTION DU MINUTEUR", "دقة المؤقت" } },
            { "monitorPhase", new[] { "RAM MONITOR", "RAM モニター", "RAM 모니터 시작", "RAM 监控", "SURVEILLANCE RAM", "مراقبة الذاكرة" } },
{ "tweaksPhase", new[] { "GAMING TWEAKS", "ゲーム最適化", "게임 트윅 적용", "应用游戏优化", "AJUSTEMENTS JEUX", "تطبيق تحسينات الألعاب" } },
            { "boostActive", new[] { "BOOST ACTIVE", "ブースト有効", "부스트 활성", "加速已激活", "BOOST ACTIF", "التعزيز نشط" } },
            { "boostRestored", new[] { "RESTORED", "復元完了", "복원 완료", "已还原", "RESTAURÉ", "تمت الاستعادة" } },
            { "monitorFreedFmt", new[] { "RAM MONITOR: +{0} MB FREED", "RAM モニター: +{0} MB 解放", "RAM 모니터: +{0} MB 해방", "RAM 监控: 已释放 {0} MB", "SURVEILLANCE RAM : +{0} Mo LIBÉRÉS", "مراقبة الذاكرة: تم تحرير {0} م.ب" } },
            { "boostNotice", new[] { "BOOST IS FULLY REVERTED BY REVERT BOOST OR WHEN THE APP CLOSES", "ブーストは「ブースト解除」またはアプリ終了時に完全に解除されます", "부스트는 부스트 해제 또는 앱 종료 시 완전히 해제됩니다", "点击撤销加速或关闭应用时将完全撤销加速", "Le boost est entièrement annulé par ANNULER LE BOOST ou à la fermeture de l'application", "يتم التراجع عن التعزيز بالكامل عند الإلغاء أو إغلاق التطبيق" } },
            { "exitWarnTitle", new[] { "BOOST ACTIVE", "ブースト有効", "부스트 활성", "加速已激活", "BOOST ACTIF", "التعزيز نشط" } },
            { "exitWarn", new[] { "A performance boost is active.\n\nAll boosted settings will be reverted now. Exit anyway?", "ブーストが有効です。\n\n設定を元に戻してから終了します。終了しますか？", "부스트가 활성 상태입니다.\n\n모든 설정을 복원한 후 종료합니다. 종료하시겠습니까?", "加速已激活。\n\n将撤销所有加速设置后再退出。确定退出吗？", "Un boost est actif.\n\nTous les réglages seront annulés avant de quitter. Quitter quand même ?", "التعزيز نشط.\n\nستتم استعادة جميع الإعدادات الآن. هل تريد الخروج؟" } },
            { "junk", new[] { "JUNK CLEANUP", "ジャンク削除", "정크 정리", "垃圾清理", "NETTOYAGE DE FICHIERS", "تنظيف الملفات" } },
            { "junkForm", new[] { "JUNK CLEANUP", "ジャンク削除", "정크 정리", "垃圾清理", "NETTOYAGE DE FICHIERS", "تنظيف الملفات" } },
            { "tempWin", new[] { "Windows temp folder", "Windows 一時フォルダ", "Windows 임시 폴더", "Windows 临时文件夹", "Dossier temporaire Windows", "مجلد Windows المؤقت" } },
            { "tempUser", new[] { "User temp folder", "ユーザー一時フォルダ", "사용자 임시 폴더", "用户临时文件夹", "Dossier temporaire utilisateur", "مجلد المستخدم المؤقت" } },
            { "wuCache", new[] { "Windows Update downloads", "Windows Update ダウンロード", "Windows Update 다운로드", "Windows Update 下载缓存", "Téléchargements Windows Update", "تنزيلات Windows Update" } },
            { "scanningRow", new[] { "SCANNING", "スキャン中", "스캔 중", "扫描中", "ANALYSE", "جارٍ الفحص" } },
            { "cleaningRow", new[] { "CLEANING", "クリーニング中", "정리 중", "清理中", "NETTOYAGE", "جارٍ التنظيف" } },
            { "cleanJunk", new[] { "CLEAN", "クリーニング", "정리", "清理", "NETTOYER", "تنظيف" } },
            { "freedFmt", new[] { "FREED {0} • {1} SKIPPED", "{0} 解放 • {1} スキップ", "{0} 해방 • {1} 건너뜀", "已释放 {0} • 跳过 {1} 项", "{0} LIBÉRÉS • {1} IGNORÉS", "تم تحرير {0} • تم تخطي {1}" } },
            { "totalLabel", new[] { "TOTAL", "合計", "합계", "总计", "TOTAL", "الإجمالي" } },
            { "amdGpuTip", new[] { "AMD GPU detected - AMD has no safe command-line automation.\nOpen AMD Software: Adrenalin Edition and set the\ngraphics profile to Gaming or eSports (performance).", "AMD GPU が検出されました。自動制御は利用できません。\nAMD Software: Adrenalin Edition を開き、\nグラフィックス プロファイルを「ゲーム」または「eスポーツ」に設定してください。", "AMD GPU가 감지되었습니다. 자동 제어가 불가능합니다.\nAMD Software: Adrenalin Edition을 열고\n그래픽 프로필을 \"게임\" 또는 \"e스포츠\"(성능 우선)로 설정하세요.", "检测到 AMD GPU - 无法自动控制。\n请打开 AMD Software: Adrenalin Edition 并将\n图形配置文件设置为 Gaming 或 eSports(性能优先)。", "GPU AMD détecté - pas d'automatisation en ligne de commande.\nOuvrez AMD Software: Adrenalin Edition et définissez le\nprofil graphique sur Gaming ou eSports (performance).", "تم اكتشاف GPU AMD - لا يوجد أتمتة آمنة لسطر الأوامر.\nافتح AMD Software: Adrenalin Edition واضبط ملف\nتعريف الرسومات على Gaming أو eSports (الأداء)." } },
            { "tweaks", new[] { "TWEAKS", "その他の設定", "추가 설정", "高级调整", "AJUSTEMENTS", "تحسينات" } },
            { "tweaksForm", new[] { "GAMING TWEAKS", "ゲーム最適化設定", "게임 최적화 설정", "游戏优化调整", "AJUSTEMENTS JEU", "تحسينات الألعاب" } },
            { "applyTweaks", new[] { "APPLY", "適用", "적용", "应用", "APPLIQUER", "تطبيق" } },
            { "revertTweaks", new[] { "REVERT ALL", "すべて解除", "모두 해제", "全部撤销", "TOUT ANNULER", "إلغاء الكل" } },
            { "tweaksNote", new[] { "Fully reverted by REVERT BOOST or when the app closes.", "REVERT BOOST またはアプリ終了時に完全に解除されます。", "REVERT BOOST 또는 앱 종료 시 완전히 해제됩니다.", "点击撤销加速或关闭应用时完全撤销。", "Entièrement annulé par REVERT BOOST ou à la fermeture.", "يتم التراجع بالكامل عبر REVERT BOOST أو عند الإغلاق." } },
            { "tweaksApplied", new[] { "APPLIED", "適用完了", "적용 완료", "已应用", "APPLIQUÉ", "تم التطبيق" } },
            { "tw0", new[] { "Game Mode", "ゲームモード", "게임 모드", "游戏模式", "Mode Jeu", "وضع اللعبة" } },
            { "tw0d", new[] { "Gives the running game CPU and I/O priority", "ゲームにCPUとI/Oの優先度を与えます", "실행 중인 게임에 CPU·I/O 우선권을 부여합니다", "为游戏提供 CPU 和 I/O 优先级", "Donne la priorité CPU/ES au jeu en cours", "منح اللعبة أولوية المعالج والإدخال/الإخراج" } },
            { "tw1", new[] { "Disable Game DVR recording", "ゲーム録画を無効化", "게임 DVR 비활성화", "关闭游戏录制", "Désactiver Game DVR", "تعطيل تسجيل الألعاب" } },
            { "tw1d", new[] { "Stops Xbox background capture that silently costs FPS", "バックグラウンド録画を停止しFPSを守ります", "백그라운드 녹화를 꺼 FPS 손실을 막습니다", "关闭占用帧率的后台录制", "Stoppe la capture en arrière-plan qui coûte des FPS", "إيقاف التسجيل الخلفي الذي يستهلك الأداء" } },
            { "tw3", new[] { "Low-latency network", "低レイテンシネットワーク", "저지연 네트워크", "低延迟网络", "Réseau faible latence", "شبكة منخفضة الكمون" } },
            { "tw3d", new[] { "Removes packet throttling for steadier ping", "パケット抑制を解除しpingを安定させます", "패킷 제한을 해제해 핑을 안정화합니다", "取消数据包限流，稳定延迟", "Supprime la limitation réseau pour un ping stable", "إزالة تقييد الحزم لثبات الاستجابة" } },
            { "tw4", new[] { "Foreground app priority", "前面ウィンドウ優先", "포그라운드 우선", "前台优先", "Priorité premier plan", "أولوية التطبيقات الأمامية" } },
            { "tw4d", new[] { "Reserves less CPU for background services", "バックグラウンドサービスのCPU予約を削減", "백그라운드 서비스의 CPU 예약을 줄입니다", "减少后台服务的 CPU 占用", "Réserve moins de CPU aux services d'arrière-plan", "حجز معالج أقل لخدمات الخلفية" } },
            { "tw5", new[] { "PCIe / USB / CPU boost power", "PCIe / USB / CPU 電力設定", "PCIe / USB / CPU 전원 최적화", "PCIe / USB / CPU 供电优化", "Alimentation PCIe / USB / CPU", "طاقة PCIe / USB / CPU" } },
            { "tw5d", new[] { "PCIe ASPM and USB suspend off, aggressive CPU boost", "PCIe/USB省電力を無効化しCPUブーストを強化", "PCIe·USB 절전을 끄고 CPU 부스트를 공격적으로", "关闭 PCIe/USB 节电，CPU 加速激进", "ASPM et USB suspend off, boost CPU agressif", "تعطيل توفير طاقة PCIe/USB وتعزيز CPU" } },
{ "tw6", new[] { "Game packet priority (QoS)", "ゲームパケット優先 (QoS)", "게임 패킷 우선 (QoS)", "游戏数据包优先 (QoS)", "Priorité paquets jeu (QoS)", "أولوية حزم اللعبة (QoS)" } },
{ "tw6d", new[] { "Tags game traffic as priority (DSCP 46) so routers and Windows keep ping steady under load", "ゲーム通信を優先(DSCP 46)し、負荷時もpingを安定させます", "게임 트래픽을 우선(DSCP 46) 처리해 부하 중에도 핑을 안정화합니다", "将游戏流量标记为优先(DSCP 46)，高负载下延迟更稳", "Marque le trafic du jeu prioritaire (DSCP 46) pour un ping stable en charge", "يوسم حركة مرور اللعبة كأولوية (DSCP 46) لثبات الاستجابة تحت الحمل" } },
            { "countFmt", new[] { "APPLIED  {0} / 6", "{0} / 6 適用済み", "{0} / 6 적용됨", "已应用 {0} / 6", "{0} / 6 APPLIQUÉS", "تم تطبيق {0} / 6" } },
            { "appliedRow", new[] { "APPLIED", "適用済み", "적용됨", "已应用", "APPLIQUÉ", "مطبق" } },
            { "onRow", new[] { "ON", "オン", "켜짐", "开", "ACTIF", "مفعل" } },
            { "offRow", new[] { "OFF", "オフ", "꺼짐", "关", "INACTIF", "معطل" } },
            { "errDenied", new[] { "Access denied", "アクセスが拒否されました", "액세스가 거부되었습니다", "访问被拒绝", "Accès refusé", "تم رفض الوصول" } },
            { "sessionFmt", new[] { "{0} SERVICES STOPPED • {1} RAM FREED", "{0} サービス停止 • メモリ {1} 解放", "서비스 {0}개 중지 • RAM {1} 해방", "已停止 {0} 项服务 • 释放 {1} 内存", "{0} SERVICES ARRÊTÉS • {1} DE RAM LIBÉRÉS", "تم إيقاف {0} خدمة • تم تحرير {1} من الذاكرة" } },
            { "agent", new[] { "AUTO", "オート", "자동", "自动", "AUTO", "تلقائي" } },
            { "agentForm", new[] { "GAME AGENT", "ゲームエージェント", "게임 에이전트", "游戏代理", "AGENTE DE JEU", "وكيل الألعاب" } },
            { "agentHint", new[] { "The agent watches these game executables. When a game is in the foreground it applies the boost automatically, and reverts when you leave the game.", "エージェントはこれらのゲームを監視します。ゲームが前面にある間ブーストし、離れると自動解除します。", "에이전트가 이 게임들을 감시합니다. 게임이 실행 중이면 자동으로 부스트하고, 종료하면 해제합니다.", "代理程序监视这些游戏。游戏在前台时自动加速，离开时自动还原。", "L'agent surveille ces jeux. Boost automatique quand un jeu est au premier plan, revert à la fermeture.", "يراقب الوكيل هذه الألعاب. يعزز الأداء تلقائيًا أثناء اللعب ويراجع عند الخروج." } },
            { "agentAdd", new[] { "ADD GAME", "ゲームを追加", "게임 추가", "添加游戏", "AJOUTER UN JEU", "إضافة لعبة" } },
            { "agentRemove", new[] { "REMOVE", "削除", "제거", "移除", "RETIRER", "إزالة" } },
            { "agentAuto", new[] { "Auto-boost when a game starts; revert when it exits", "ゲーム起動時にブーストし、終了時に解除する", "게임 실행 시 부스트, 종료 시 해제", "游戏启动时自动加速，退出时还原", "Boost auto au lancement du jeu, revert à la fermeture", "تعزيز تلقائي عند بدء اللعبة والتراجع عند الخروج" } },
            { "agentStartup", new[] { "Run HyperBoost at Windows startup", "Windows 起動時に HyperBoost を実行", "Windows 시작 시 HyperBoost 실행", "Windows 启动时运行 HyperBoost", "Lancer HyperBoost au démarrage de Windows", "تشغيل HyperBoost عند بدء تشغيل Windows" } },
            { "recoverTitle", new[] { "RECOVERY", "復元", "복구", "恢复", "RÉCUPÉRATION", "استعادة" } },
            { "recoverMsg", new[] { "Previous boost was interrupted (crash or force-kill).\nRestore your original power plan and settings now?", "前回のブーストが中断されました（クラッシュまたは強制終了）。\n元の電源プランと設定を今すぐ復元しますか？", "이전 부스트가 중단되었습니다(충돌 또는 강제 종료).\n원래 전원 계획과 설정을 지금 복원하시겠습니까?", "上次加速被中断（崩溃或强制结束）。\n立即恢复原始电源计划和设置吗？", "Le boost précédent a été interrompu (plantage ou arrêt forcé).\nRestaurer votre plan d'alimentation et vos réglages maintenant ?", "تمت مقاطعة التعزيز السابق (تعطل أو إنهاء قسري).\nاستعادة خطة الطاقة والإعدادات الأصلية الآن؟" } },
            { "trayOpen", new[] { "Open HyperBoost", "HyperBoost を開く", "HyperBoost 열기", "打开 HyperBoost", "Ouvrir HyperBoost", "فتح HyperBoost" } },
            { "trayExit", new[] { "Exit", "終了", "종료", "退出", "Quitter", "خروج" } },
            { "errDependents", new[] { "Dependent services running", "依存サービスが実行中です", "종속 서비스 실행 중", "依赖服务正在运行", "Services dépendants actifs", "خدمات تابعة قيد التشغيل" } },
            { "errUnresponsive", new[] { "Unresponsive", "応答がありません", "응답 없음", "无响应", "Sans réponse", "لا استجابة" } },
            { "errGeneric", new[] { "Operation failed", "操作に失敗しました", "작업 실패", "操作失败", "Échec de l'opération", "فشلت العملية" } },
            { "statusRunning", new[] { "Running", "実行中", "실행 중", "运行中", "En cours", "قيد التشغيل" } },
            { "statusStopped", new[] { "Stopped", "停止済み", "중지됨", "已停止", "Arrêté", "متوقف" } },
            { "statusNotInstalled", new[] { "Not installed", "未インストール", "설치되지 않음", "未安装", "Non installé", "غير مثبت" } },
            { "statusStarting", new[] { "Starting...", "開始中...", "시작 중...", "正在启动...", "Démarrage...", "جارٍ البدء..." } },
            { "statusStopping", new[] { "Stopping...", "停止中...", "중지 중...", "正在停止...", "Arrêt...", "جارٍ الإيقاف..." } },
            { "statusPaused", new[] { "Paused", "一時停止", "일시 중지", "已暂停", "En pause", "متوقف مؤقتًا" } }
        };

        internal static string T(AppLanguage l, string key)
        {
            string[] values;
            return table.TryGetValue(key, out values) ? values[(int)l] : key;
        }
    }
}
