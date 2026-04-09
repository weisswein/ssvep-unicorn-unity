// 5. Unity での使い方

// Hierarchy に空オブジェクトを 3 つ作って、

// NetworkManager
// UnicornUdpReceiver
// UnicornTriggerSender
// Logger
// SsvepEventLogger
// Experiment
// SsvepExperimentController

// を付けます。

// SsvepExperimentController の Inspector で

// triggerSender に NetworkManager
// eventLogger に Logger

// を割り当てます。

// 6. 受信データの列の見方

// Recorder の既定順は、EEG 8ch のあとに加速度・ジャイロ・Counter・Battery・Validation・Delta Time・Status/Trigger です。つまり、全部有効にしていれば典型的には次の順になります。

// sig_0 ～ sig_7 : EEG
// sig_8 ～ sig_10 : ACC
// sig_11 ～ sig_13 : GYR
// sig_14 : Counter
// sig_15 : Battery
// sig_16 : Validation
// sig_17 : Delta Time
// sig_18 : Status/Trigger

// 最初に必ず、Unity が保存した CSV を見て 本当にこの並びになっているか を確認してください。
// Signal Selection を変えると列数が変わるので、ここを固定せずに解析するとずれます。

// 7. 解析用データとして何を残すべきか

// この構成なら最低限、次の 3 種類が残ります。

// Recorder の CSV
// Unity の raw_udp_...csv
// Unity の events_...csv

// 学習準備として重要なのは events_...csv で、

// trial_id
// class_id
// freq_hz
// start / end 時刻

// があることです。
// これであとから 0.2–2.2 秒や 0.5–3.0 秒などの窓を切り出せます。

// 8. 実際の研究としての評価

// 可能か不可能か
// 可能です。Unity だけで、トリガ送信・UDP受信・イベント記録 までまとめられます。Recorder も UDP/LSL 出力と CSV/BDF 保存を想定しているので、この構成は素直です。

// 正しい内容か
// 正しいです。特に SSVEP では、分類器より前に 刺激同期とイベントログ を固めるのが重要です。README でも、刺激提示ツールから Recorder に UDP トリガを送り、データとトリガを UDP/LSL で外部処理する研究用途が示されています。

// 新規性
// この段階は基盤実装なので新規性は高くありません。
// ただし、ここから

// Unity の VR/3D タスクに統合する
// ゲーム中の並行操作で SSVEP を使う
// 実時間 FBCCA / TRCA を回す

// まで行けば、研究としての面白さはかなり出ます。

// 9. つまずきやすい原因と改善点

// 原因はだいたい次です。

// UDP ペイロード形式が想定と違う
// 列のどれが EEG か確定していない
// Unity の trial 開始時刻と実刺激表示時刻がずれる
// 点滅周波数は合っていても画面フレームと一致していない
// 窓長が短すぎて SSVEP が弱い

// 改善は次です。

// まず Unity 側で raw packet を全部保存する
// sig_0〜sig_7 が EEG かを Recorder の設定と照合する
// 最初は 2〜3 秒窓でオフライン確認する
// trial start 後 0.2〜0.5 秒を捨てて切り出す
// リアルタイム分類は最後に足す