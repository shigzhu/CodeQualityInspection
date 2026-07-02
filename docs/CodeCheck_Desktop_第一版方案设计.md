# CodeCheck Desktop 第一版方案设计

> 本文用于保存当前阶段关于 `CodeCheck Desktop` 的需求、架构和设计结论，避免后续会话上下文丢失。

---

## 1. 产品定位

`CodeCheck Desktop` 是一个运行在 `Windows 10` 上的 C/C++ 代码质量检查桌面工具。

第一版目标：

- 面向团队代码评审场景。
- 工程人员在个人 PC 上运行。
- 支持扫描 C/C++ 工程目录。
- 支持单独选择一个或多个文件扫描。
- 支持显式选择 `.h/.hpp/.hh/.hxx` 头文件扫描。
- 输出中文质量报告。
- 底层封装开源检查工具，避免从零开发静态分析引擎。
- 不做独立安装包，所有执行文件、配置、依赖、规则、报告模板放在同一个 `release` 目录中，方便后续手动打包。

---

## 2. 技术选型

### 2.1 桌面端

- 技术：`C# WPF + WebView2`
- 入口程序：`CodeCheck.Desktop.exe`
- 用途：配置扫描、启动扫描、展示进度、查看报告、管理基线和误报。

### 2.2 扫描内核

- 技术：`.NET 8` 命令行程序
- 入口程序：`CodeCheck.Cli.exe`
- 用途：执行扫描、调用第三方工具、合并结果、生成报告。

### 2.3 第三方检查工具

第一版内置：

- `LLVM/Clang-Tidy`
- `Cppcheck`
- `Lizard`

其中：

- `clang-tidy`：AST 级 C/C++ 检查，CERT、bugprone、cppcoreguidelines 等。
- `cppcheck`：低误报静态分析，空指针、未初始化、内存、冗余代码等。
- `lizard`：圈复杂度、函数长度等度量指标。

---

## 3. 规则来源和规则策略

### 3.1 规则来源

第一版规则只采用：

1. `C++Languagelawer.pdf` 中的华为 C/C++ 编程规范；
2. `CERT C`；
3. `CERT C++`。

暂不加入：

- 团队自定义规则；
- MISRA；
- AUTOSAR；
- Google C++ Style；
- LLVM Style。

### 3.2 规则优先级

如果华为规范和 CERT 冲突：

```text
CERT C/C++ > 华为 C/C++ 编程规范
```

处理方式：

- 华为规则与 CERT 一致：保留或合并说明。
- 华为规则比 CERT 更严格：可保留，但允许降级。
- 华为规则与 CERT 冲突：以 CERT 为准，华为规则可降级为建议。

### 3.3 规则数量

第一版固定为 `100` 条规则。

建议分布：

| 来源 | 数量 |
|---|---:|
| 华为 C 规范 | 22 |
| 华为 C++ 规范 | 38 |
| CERT C | 20 |
| CERT C++ | 20 |
| 合计 | 100 |

### 3.4 规则编号

采用：

```text
Quectel-C-xxx
Quectel-CPP-xxx
Quectel-CERT-C-xxx
Quectel-CERT-CPP-xxx
```

示例：

```text
Quectel-C-023：函数圈复杂度不得超过 10
Quectel-C-024：函数体代码行数不得超过 100 行
Quectel-CPP-038：函数圈复杂度不得超过 10
Quectel-CPP-039：函数体代码行数不得超过 100 行
```

### 3.5 规则严重级别

| 中文 | 英文 | 含义 |
|---|---|---|
| 阻断 | `Blocker` | 高风险，必须优先修复 |
| 严重 | `Critical` | 可能导致崩溃、安全问题、严重缺陷 |
| 警告 | `Warning` | 需要评估和整改 |
| 建议 | `Suggestion` | 风格、可维护性建议 |

### 3.6 每条规则必须包含

- 规则编号；
- 规则名称；
- 规则来源；
- 适用语言；
- 严重级别；
- 中文说明；
- 中文错误示例；
- 中文正确示例；
- 中文修复建议；
- 检测方式；
- 是否默认启用；
- 是否允许关闭；
- 关闭风险说明。

---

## 4. 重点新增规则

### 4.1 圈复杂度规则

要求：

```text
圈复杂度 <= 10
```

实现方式：

- 使用 `lizard` 统计函数圈复杂度。
- `CodeCheck` 根据阈值判断是否生成问题。

规则：

```text
Quectel-C-023：函数圈复杂度不得超过 10
Quectel-CPP-038：函数圈复杂度不得超过 10
```

级别：`Warning`

### 4.2 函数体代码行数规则

要求：

```text
函数体代码行数 <= 100
```

实现方式：

- 使用 `lizard` 统计函数长度。
- `CodeCheck` 根据阈值判断是否生成问题。

规则：

```text
Quectel-C-024：函数体代码行数不得超过 100 行
Quectel-CPP-039：函数体代码行数不得超过 100 行
```

级别：`Warning`

---

## 5. SonarQube 讨论结论

`SonarQube` 和 `clang-tidy`、`cppcheck` 定位不同。

```text
clang-tidy / cppcheck：静态分析引擎
SonarQube：代码质量管理平台
```

第一版结论：

- 不引入 `SonarQube` 本体。
- 不依赖 `SonarQube CFamily` 分析器。
- 借鉴其质量管理思想。

可借鉴内容：

- `Quality Profile`：对应 `rule-profiles.json`；
- `New Code`：对应 `baseline.json`；
- `False Positive`：对应 `suppression.json`；
- `Quality Gate`：第一版不做，后续可加；
- 复杂度指标：通过 `lizard` 实现；
- 质量仪表盘：通过本地 HTML 报告和 GUI 实现。

---

## 6. 扫描对象和语言策略

### 6.1 支持扫描对象

- 工程目录；
- 单个 `.c/.cpp/.cc/.cxx/.h/.hpp/.hh/.hxx` 文件；
- 多个文件。

### 6.2 目录扫描头文件策略

默认：

```json
"headerScanPolicy": {
  "scanHeadersInDirectoryMode": false,
  "allowHeaderAsExplicitInput": true,
  "headerLanguageMode": "auto"
}
```

含义：

- 目录扫描时默认不把头文件作为入口；
- 用户显式选择头文件时允许扫描；
- 头文件语言模式默认自动判断。

### 6.3 C/C++ 标准

- `.c`：按 C 规则检查，建议默认 `c11`；
- `.cpp/.cc/.cxx/.hpp/.hh/.hxx`：按 C++14 检查；
- `.h`：可按自动、C 或 C++14 解析。

---

## 7. 编译上下文要求

用户已确认：

```text
普通目录扫描时，如果没有 include 路径，不允许降级扫描。
```

因此扫描前必须获取：

- include 路径；
- 宏定义；
- C/C++ 标准；
- Qt 路径，若为 Qt 项目；
- `.vcxproj/.pro` 工程配置，若可用。

支持从以下来源加载：

- 用户手动输入；
- 根据选择的文件路径推断；
- `.vcxproj`；
- `.pro`。

扫描失败文件必须显示在报告中。

---

## 8. 默认排除规则

默认不扫描第三方代码和生成文件。

默认排除目录：

```text
third_party
3rdparty
external
extern
vendor
vendors
dependencies
deps
packages
build
out
bin
obj
Debug
Release
x64
x86
.git
.svn
.vs
```

默认排除 Qt 生成文件：

```text
moc_*.cpp
ui_*.h
qrc_*.cpp
mocs_compilation.cpp
*_autogen
```

---

## 9. 配置文件 `codecheck.json`

顶层结构：

```json
{
  "version": "1.0.0",
  "project": {},
  "input": {},
  "build": {},
  "scan": {},
  "engines": {},
  "rules": {},
  "baseline": {},
  "suppression": {},
  "report": {},
  "runtime": {}
}
```

关键配置：

```json
{
  "build": {
    "languageStandard": {
      "c": "c11",
      "cpp": "c++14"
    },
    "loadFromProjectFile": true,
    "supportedProjectTypes": ["vcxproj", "pro"],
    "requireCompileContext": true,
    "allowDegradedScan": false
  },
  "input": {
    "headerScanPolicy": {
      "scanHeadersInDirectoryMode": false,
      "allowHeaderAsExplicitInput": true,
      "headerLanguageMode": "auto"
    }
  },
  "engines": {
    "clangTidy": {
      "enabled": true,
      "path": "tools\\llvm\\bin\\clang-tidy.exe"
    },
    "cppcheck": {
      "enabled": true,
      "path": "tools\\cppcheck\\cppcheck.exe"
    },
    "lizard": {
      "enabled": true,
      "path": "tools\\lizard\\lizard.exe",
      "thresholds": {
        "cyclomaticComplexity": 10,
        "functionLines": 100
      }
    },
    "builtin": {
      "enabled": true
    }
  },
  "rules": {
    "ruleIndex": "rules\\rules.index.json",
    "profile": "default"
  },
  "baseline": {
    "enabled": true,
    "mode": "compare",
    "createIfMissing": true
  },
  "suppression": {
    "enabled": true,
    "allowSourceCommentSuppression": false
  },
  "report": {
    "formats": ["html", "sarif", "json"],
    "optionalFormats": ["xlsx"],
    "generateXlsx": false,
    "language": "zh-CN"
  }
}
```

---

## 10. 报告文件 `report.json`

`report.json` 是所有报告的核心数据源。

顶层结构：

```json
{
  "schemaVersion": "1.0.0",
  "reportId": "",
  "tool": {},
  "project": {},
  "scan": {},
  "summary": {},
  "qualityScore": {},
  "metrics": {},
  "rules": {},
  "issues": [],
  "failedFiles": [],
  "disabledRules": [],
  "suppressedIssues": [],
  "baseline": {},
  "outputs": {},
  "logs": []
}
```

报告支持：

- 扫描摘要；
- 质量评分；
- 问题明细；
- 代码片段；
- 修复建议；
- 扫描失败文件；
- 已关闭规则清单；
- 误报抑制；
- 基线对比；
- 复杂度指标；
- `HTML/SARIF/JSON/XLSX` 生成。

### 10.1 质量评分

采用 100 分制。

初版扣分规则：

```text
基础分：100
阻断：每个扣 10 分
严重：每个扣 5 分
警告：每个扣 2 分
建议：每个扣 0.5 分
最低：0 分
```

评分只计算未抑制的 Active 问题。

### 10.2 复杂度指标

`report.json` 增加：

```json
"metrics": {
  "engine": "lizard",
  "summary": {
    "functionCount": 0,
    "maxCyclomaticComplexity": 0,
    "averageCyclomaticComplexity": 0,
    "maxFunctionLines": 0
  },
  "topComplexFunctions": [],
  "topLongFunctions": []
}
```

---

## 11. 报告输出格式

默认生成：

- `report.json`
- `report.html`
- `report.sarif`

可选导出：

- `report.xlsx`

Excel 导出格式为 `.xlsx`。

Excel 建议包含 Sheet：

- 扫描摘要；
- 问题明细；
- 已抑制问题；
- 扫描失败文件；
- 已关闭规则。

---

## 12. 基线 `baseline.json`

第一版必须支持基线。

用途：

- 判断新增问题；
- 判断历史问题；
- 判断已修复问题。

顶层结构：

```json
{
  "schemaVersion": "1.0.0",
  "baselineId": "",
  "project": {},
  "tool": {},
  "createdAt": "",
  "updatedAt": "",
  "mode": "snapshot",
  "summary": {},
  "issues": []
}
```

### 12.1 首次扫描自动创建基线

用户已确认：

```text
首次建立基线不需要用户手动点击创建，默认自动创建。
```

逻辑：

- 如果 `baseline.enabled = true`；
- 且 baseline 文件不存在；
- 且 `createIfMissing = true`；
- 扫描完成后自动创建初始基线。

首次扫描问题状态：

```text
NotCompared
```

后续扫描才区分：

```text
New / Existing / Fixed
```

---

## 13. 误报抑制 `suppression.json`

第一版必须支持误报抑制。

用途：

- 标记误报；
- 从活跃问题和质量评分中排除；
- 记录谁、何时、为什么抑制。

顶层结构：

```json
{
  "schemaVersion": "1.0.0",
  "suppressionId": "",
  "project": {},
  "createdAt": "",
  "updatedAt": "",
  "suppressions": []
}
```

支持三种抑制范围：

- 单个问题：`fingerprint`；
- 文件 + 规则：`file-rule`；
- 目录 + 规则：`path-rule`。

第一版不支持源码注释抑制，避免污染源码。

已抑制问题：

- 不参与质量评分；
- 不写入新基线；
- 仍在报告中单独展示。

---

## 14. 指纹 fingerprint

用于：

- 基线对比；
- 误报抑制；
- 判断问题是否是同一个。

建议生成：

```text
primaryFingerprint
stableFingerprint
```

第一版主用 `stableFingerprint`。

建议字段：

```text
ruleId
relativeFile
normalizedMessage
normalizedCodeLine
functionName 可选
```

---

## 15. CLI 设计

`CodeCheck.Cli.exe` 命令：

```text
CodeCheck.Cli.exe scan
CodeCheck.Cli.exe validate
CodeCheck.Cli.exe baseline create/update/compare
CodeCheck.Cli.exe suppress add/disable/list
CodeCheck.Cli.exe export html/sarif/xlsx
```

### 15.1 扫描命令示例

```text
CodeCheck.Cli.exe scan --config configs\default-codecheck.json
```

扫描目录：

```text
CodeCheck.Cli.exe scan --config configs\default-codecheck.json --input-type directory --path "D:\项目代码\DemoProject"
```

扫描多文件：

```text
CodeCheck.Cli.exe scan --config configs\default-codecheck.json --input-type file-list --file-list "D:\Temp\codecheck-files.txt"
```

### 15.2 扫描流程

```text
读取配置
合并命令行参数
校验配置
加载规则库
加载 suppression
加载 baseline
发现输入文件
排除第三方目录和生成文件
识别语言
解析 .vcxproj/.pro
构建编译上下文
调用 clang-tidy
调用 cppcheck
调用 lizard
执行 builtin 规则
解析并合并问题
生成 fingerprint
应用 suppression
应用 baseline
计算质量评分
生成 report.json
生成 report.html
生成 report.sarif
可选生成 report.xlsx
```

### 15.3 暂停、继续、取消

GUI 通过控制文件控制 CLI：

```text
.codecheck-control.json
```

支持命令：

```json
{ "command": "pause" }
{ "command": "resume" }
{ "command": "cancel" }
```

暂停策略：

- 不强制挂起第三方进程；
- 当前文件扫描完成后暂停任务队列。

取消策略：

- 停止任务队列；
- 尝试终止当前子进程；
- 保存已有结果；
- 生成 `Cancelled` 状态报告。

### 15.4 进度输出

CLI 输出 JSON Lines，供 GUI 实时解析：

```json
{"type":"scan-started","project":"DemoProject"}
{"type":"file-started","current":1,"total":128,"file":"src\\main.cpp"}
{"type":"issue-found","severity":"Critical","ruleId":"Quectel-CERT-C-001"}
{"type":"scan-completed","status":"Completed","report":"reports\\DemoProject\\report.json"}
```

---

## 16. GUI 设计

### 16.1 主要页面

- 首页；
- 新建扫描；
- 扫描配置；
- 规则管理；
- 扫描进度；
- 扫描结果；
- 报告预览；
- 基线管理；
- 误报管理；
- 设置。

### 16.2 普通扫描流程

规则集选择不作为普通流程步骤。

普通流程：

```text
打开 CodeCheck Desktop
点击新建扫描
选择工程目录 / 单文件 / 多文件
配置 include 路径、宏定义、Qt 路径
系统自动使用默认规则集 default
点击开始扫描
扫描完成
查看扫描结果
查看 HTML 报告
必要时导出 Excel
```

特殊情况下才进入高级规则配置修改规则。

### 16.3 首次扫描流程

```text
打开 CodeCheck Desktop
选择扫描对象
配置编译上下文
开始扫描
扫描完成
系统自动创建初始基线
查看结果
```

GUI 提示：

```text
首次扫描完成，已自动创建初始基线。后续扫描将自动识别新增问题、历史问题和已修复问题。
```

### 16.4 后续扫描流程

```text
再次扫描
自动加载已有 baseline
报告区分：新增问题、历史问题、已修复问题
```

### 16.5 规则管理

默认使用 `default` 规则集。

规则修改入口放在：

- 设置；
- 或扫描配置页的“高级规则配置”。

关闭规则必须：

- 填写原因；
- 如果关闭 `Blocker/Critical`，必须确认风险；
- 报告中显示已关闭规则清单；
- 高风险关闭规则标红。

---

## 17. HTML 报告展示内容

报告包含：

- 扫描摘要；
- 质量评分；
- 严重级别分布；
- 新增/历史/已修复问题；
- 已抑制问题数量；
- 扫描失败文件；
- 已关闭规则清单；
- 问题明细；
- 中文修复建议；
- 复杂度指标；
- 圈复杂度 Top 10；
- 函数长度 Top 10。

第一版 HTML 报告不需要公司 Logo。

---

## 18. release 目录结构

用户确认：不做单独安装包，所有内容放在同一个 `release` 目录中。

推荐结构：

```text
release
├── CodeCheck.Desktop.exe
├── CodeCheck.Cli.exe
├── CodeCheck.Desktop.dll
├── CodeCheck.Cli.dll
├── CodeCheck.Core.dll
├── CodeCheck.Reporting.dll
├── appsettings.json
│
├── tools
│   ├── llvm
│   ├── cppcheck
│   └── lizard
│       └── lizard.exe
│
├── rules
│   ├── rules.index.json
│   ├── quectel-c-rules.json
│   ├── quectel-cpp-rules.json
│   ├── cert-c-rules.json
│   ├── cert-cpp-rules.json
│   ├── rule-mapping.json
│   └── rule-profiles.json
│
├── configs
│   ├── default-codecheck.json
│   ├── default-excludes.json
│   ├── default-severity.json
│   └── templates
│
├── report-templates
│   ├── html
│   └── sarif
│
├── reports
├── baseline
├── suppressions
├── logs
├── temp
│
├── licenses
│   ├── LLVM-LICENSE.txt
│   ├── Cppcheck-LICENSE.txt
│   ├── Lizard-LICENSE.txt
│   ├── WebView2-LICENSE.txt
│   └── THIRD-PARTY-NOTICES.txt
│
├── docs
│   ├── README.md
│   ├── 使用说明.md
│   ├── 规则说明.md
│   ├── 第三方组件说明.md
│   └── 常见问题.md
│
└── samples
    ├── c-demo
    ├── cpp-demo
    └── qt-demo
```

---

## 19. 路径和部署原则

必须支持：

- 中文路径；
- 空格路径；
- release 目录移动；
- 非固定安装路径。

原则：

- 所有内部路径相对 `release` 根目录；
- 程序启动时以 `AppContext.BaseDirectory` 获取根目录；
- 调用外部进程时使用参数列表，避免拼接命令行字符串；
- 报告中优先显示相对路径。

---

## 20. 第一次启动自检

`CodeCheck.Desktop.exe` 启动时检查：

- `CodeCheck.Cli.exe` 是否存在；
- `tools\llvm\bin\clang-tidy.exe` 是否存在；
- `tools\cppcheck\cppcheck.exe` 是否存在；
- `tools\lizard\lizard.exe` 是否存在；
- `rules\rules.index.json` 是否存在；
- `configs\default-codecheck.json` 是否存在；
- `reports/baseline/suppressions/logs/temp` 是否可写；
- `WebView2 Runtime` 是否可用。

---

## 21. 第三方许可证

`release/licenses` 中必须包含：

- `LLVM-LICENSE.txt`
- `Cppcheck-LICENSE.txt`
- `Lizard-LICENSE.txt`
- `WebView2-LICENSE.txt`
- `THIRD-PARTY-NOTICES.txt`

`THIRD-PARTY-NOTICES.txt` 记录：

- 组件名称；
- 版本；
- 官网/仓库；
- 许可证；
- 用途；
- 是否修改源码。

---

## 22. 暂不做的内容

第一版暂不做：

- 邮件推送；
- 严重问题实时推送；
- Jenkins/GitLab/SVN/Git 集成；
- 权限管理；
- 多人项目管理；
- 自动更新；
- 公司 Logo；
- 问题数量阈值门禁；
- 直接引入 SonarQube 本体；
- 完整自研 C/C++ 编译前端。

---

## 23. 下一步待继续设计

后续建议继续设计：

1. 开发任务拆分和里程碑计划；
2. 工程项目结构；
3. CLI 详细模块接口；
4. GUI 页面原型和 ViewModel；
5. 规则库 100 条规则清单；
6. 第三方工具获取和 release 集成方式；
7. 测试样例和验收标准。

---

## 24. 当前结论摘要

```text
产品：CodeCheck Desktop
平台：Windows 10
界面：C# WPF + WebView2
扫描内核：CodeCheck.Cli.exe
第三方工具：LLVM/Clang-Tidy + Cppcheck + Lizard
规则来源：华为 C/C++ 编程规范 + CERT C/C++
规则数量：100 条
扫描对象：目录、单文件、多文件、显式头文件
报告：HTML、SARIF、JSON，Excel .xlsx 可选
质量评分：100 分制
基线：第一版必须做，首次扫描自动创建
误报抑制：第一版必须做
部署：release 目录复制即用，不做安装包
```

---

## 25. 开发任务拆分和里程碑计划

第一版开发建议按以下阶段推进：

```text
M0：工程骨架和基础设施
M1：规则库与配置文件
M2：CLI 扫描内核最小可用版
M3：第三方工具集成
M4：报告生成
M5：基线和误报抑制
M6：WPF 桌面端
M7：release 目录整理
M8：测试、试用、修正
```

核心原则：

```text
先 CLI，后 GUI；
先 report.json，后 HTML/SARIF/XLSX；
先能扫，再做漂亮界面；
先低误报，再扩展规则。
```

### 25.1 M0：工程骨架和基础设施

目标：建立完整解决方案结构，可以编译运行。

产物：

```text
CodeCheck.sln
src\CodeCheck.Core
src\CodeCheck.Cli
src\CodeCheck.Desktop
src\CodeCheck.Reporting
tests\CodeCheck.Tests
rules
configs
report-templates
docs
samples
release
```

验收标准：

- 解决方案可以正常编译；
- `CodeCheck.Cli.exe` 可以输出版本号；
- `CodeCheck.Desktop.exe` 可以启动空窗口；
- 可以生成基础 `release` 目录结构。

### 25.2 M1：规则库与配置文件

目标：建立第一版规则库结构和默认配置。

产物：

```text
rules\rules.index.json
rules\quectel-c-rules.json
rules\quectel-cpp-rules.json
rules\cert-c-rules.json
rules\cert-cpp-rules.json
rules\rule-mapping.json
rules\rule-profiles.json
configs\default-codecheck.json
configs\default-excludes.json
configs\default-severity.json
```

验收标准：

- `validate` 能加载规则库；
- 第一版规则总数固定为 `100`；
- `default` 规则集可以解析；
- 配置非法时输出中文错误信息。

### 25.3 M2：CLI 扫描内核最小可用版

目标：实现 `CodeCheck.Cli.exe scan` 的最小闭环。

第一阶段先实现：

- 读取配置；
- 发现待扫描文件；
- 识别 C/C++/Header；
- 排除默认目录和 Qt 生成文件；
- 生成基础 `report.json`。

验收标准：

- 能扫描目录；
- 能扫描单文件；
- 能扫描多文件列表；
- 能显式扫描 `.h/.hpp/.hh/.hxx`；
- 能生成基础 `report.json` 和 `scan.log`。

### 25.4 M3：第三方工具集成

目标：接入 `clang-tidy`、`cppcheck`、`lizard`。

开发内容：

- `ClangTidyRunner` 与输出解析；
- `CppcheckRunner` 与 XML 输出解析；
- `LizardRunner` 与复杂度指标解析；
- 将第三方结果归一化为统一 `Issue` 模型。

验收标准：

- 缺失任一工具时 `validate` 能报错；
- `clang-tidy` 能产生问题；
- `cppcheck` 能产生问题；
- `lizard` 能产生复杂度和函数长度问题；
- 单文件或单引擎失败不影响其他文件继续扫描。

### 25.5 M4：报告生成

目标：生成第一版报告文件。

产物：

```text
report.json
report.html
report.sarif
report.xlsx 可选
```

验收标准：

- `report.json` 字段完整；
- `report.html` 可用浏览器打开；
- `report.sarif` 符合 SARIF 基本结构；
- `report.xlsx` 可由 Excel 打开；
- 中文和中文路径显示正常。

### 25.6 M5：基线和误报抑制

目标：完成第一版必须功能 `baseline.json` 和 `suppression.json`。

验收标准：

- 首次扫描自动创建 baseline；
- 第二次扫描能识别 `Existing`；
- 修改代码后能识别 `New`；
- 删除问题后能识别 `Fixed`；
- 标记误报后，问题不参与质量评分；
- 抑制记录不直接删除，只标记 `Disabled`。

### 25.7 M6：WPF 桌面端

目标：实现图形界面。

页面优先级：

```text
P0：首页 + 工具状态检测
P1：新建扫描
P2：扫描配置
P3：扫描进度
P4：扫描结果
P5：报告预览
P6：误报抑制
P7：基线管理
P8：规则管理
P9：设置
```

验收标准：

- 能选择目录、单文件、多文件扫描；
- 能显式选择头文件扫描；
- 能配置 include 路径、宏定义、Qt 路径；
- 能启动 CLI 并显示进度；
- 能暂停、继续、取消；
- 能加载 `report.json`；
- 能用 WebView2 打开 `report.html`；
- 能标记误报；
- 能更新基线；
- 能导出 `.xlsx`。

### 25.8 M7：release 目录整理

目标：形成复制即用的 `release` 目录。

验收标准：

- `release` 移动路径后仍可运行；
- 中文路径下可运行；
- 空格路径下可运行；
- 不需要管理员权限；
- 不依赖固定安装目录。

### 25.9 M8：测试、试用、修正

目标：在样例工程和真实项目上试用。

测试范围：

- 配置解析；
- 规则加载；
- 文件发现；
- 排除匹配；
- 第三方工具调用；
- 指纹生成；
- 基线对比；
- 误报抑制；
- 质量评分；
- HTML/SARIF/XLSX 生成。

建议试用项目：

- 1 个 Visual Studio C++ 项目；
- 1 个 Qt 项目；
- 1 个纯 C 项目。

---

## 26. 工程项目结构和核心模块接口

### 26.1 解决方案结构

建议解决方案结构如下：

```text
CodeCheck.sln
├── src
│   ├── CodeCheck.Core
│   ├── CodeCheck.Cli
│   ├── CodeCheck.Desktop
│   └── CodeCheck.Reporting
│
├── tests
│   └── CodeCheck.Tests
│
├── rules
├── configs
├── report-templates
├── docs
├── samples
└── release
```

项目职责：

| 项目 | 职责 |
|---|---|
| `CodeCheck.Core` | 公共模型、配置、规则、Issue、基线、误报、扫描上下文、工具调用基础能力 |
| `CodeCheck.Cli` | 命令行入口，执行扫描、校验、基线、误报、导出命令 |
| `CodeCheck.Reporting` | `HTML`、`SARIF`、`XLSX`、`JSON` 报告生成 |
| `CodeCheck.Desktop` | WPF 图形界面，调用 CLI，展示进度和结果 |
| `CodeCheck.Tests` | 单元测试、集成测试、回归测试 |

### 26.2 `CodeCheck.Core` 模块结构

建议目录：

```text
CodeCheck.Core
├── Configuration
├── Rules
├── Issues
├── Baseline
├── Suppression
├── Metrics
├── Inputs
├── Build
├── Engines
├── Runtime
└── Utilities
```

核心模型：

```text
CodeCheckConfig
RuleDefinition
RuleProfile
RuleMapping
Issue
IssueLocation
IssueSnippet
FailedFile
ScanReport
QualityScore
BaselineFile
SuppressionFile
MetricSummary
FunctionMetric
ScanContext
CompileContext
ScanInputFile
```

### 26.3 配置模型

建议核心类：

```csharp
public sealed class CodeCheckConfig
{
    public string Version { get; set; }
    public ProjectConfig Project { get; set; }
    public InputConfig Input { get; set; }
    public BuildConfig Build { get; set; }
    public ScanConfig Scan { get; set; }
    public EngineConfig Engines { get; set; }
    public RuleConfig Rules { get; set; }
    public BaselineConfig Baseline { get; set; }
    public SuppressionConfig Suppression { get; set; }
    public ReportConfig Report { get; set; }
    public RuntimeConfig Runtime { get; set; }
}
```

主要服务接口：

```csharp
public interface IConfigLoader
{
    Task<CodeCheckConfig> LoadAsync(string path, CancellationToken cancellationToken);
}

public interface IConfigValidator
{
    ValidationResult Validate(CodeCheckConfig config);
}
```

### 26.4 规则模型和规则加载

核心类：

```csharp
public sealed class RuleDefinition
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Source { get; set; }
    public string SourceRuleId { get; set; }
    public IReadOnlyList<string> Language { get; set; }
    public string Category { get; set; }
    public RuleSeverity Severity { get; set; }
    public bool DefaultEnabled { get; set; }
    public bool AllowDisable { get; set; }
    public string DisableRisk { get; set; }
    public string Description { get; set; }
    public RuleExample BadExample { get; set; }
    public RuleExample GoodExample { get; set; }
    public string Suggestion { get; set; }
    public RuleDetection Detection { get; set; }
    public IReadOnlyList<string> References { get; set; }
    public IReadOnlyList<string> Tags { get; set; }
    public string Version { get; set; }
}
```

服务接口：

```csharp
public interface IRuleLoader
{
    Task<RuleSet> LoadAsync(string ruleIndexPath, CancellationToken cancellationToken);
}

public interface IRuleProfileService
{
    ActiveRuleSet BuildActiveRuleSet(RuleSet ruleSet, RuleConfig config);
}
```

### 26.5 Issue 统一模型

所有引擎输出最终都转换为统一 `Issue`。

```csharp
public sealed class Issue
{
    public string IssueId { get; set; }
    public string Fingerprint { get; set; }
    public string PrimaryFingerprint { get; set; }
    public string RuleId { get; set; }
    public string RuleTitle { get; set; }
    public string SourceRuleId { get; set; }
    public string RuleSource { get; set; }
    public RuleSeverity Severity { get; set; }
    public string Language { get; set; }
    public string Engine { get; set; }
    public string EngineRuleId { get; set; }
    public string Message { get; set; }
    public string Description { get; set; }
    public string Suggestion { get; set; }
    public IssueLocation Location { get; set; }
    public IssueSnippet CodeSnippet { get; set; }
    public BaselineState BaselineState { get; set; }
    public SuppressionState SuppressionState { get; set; }
    public bool IsSuppressed { get; set; }
    public bool IsAutoFixable { get; set; }
    public IReadOnlyList<string> Tags { get; set; }
}
```

枚举建议：

```csharp
public enum RuleSeverity
{
    Blocker,
    Critical,
    Warning,
    Suggestion
}

public enum BaselineState
{
    New,
    Existing,
    Fixed,
    NotCompared
}

public enum SuppressionState
{
    Active,
    Suppressed,
    SuppressionExpired,
    SuppressionInvalid
}
```

### 26.6 输入文件和编译上下文

核心类：

```csharp
public sealed class ScanInputFile
{
    public string FullPath { get; set; }
    public string RelativePath { get; set; }
    public string Language { get; set; }
    public bool IsHeader { get; set; }
    public bool IsExplicitInput { get; set; }
}

public sealed class CompileContext
{
    public IReadOnlyList<string> IncludeDirectories { get; set; }
    public IReadOnlyList<string> Defines { get; set; }
    public string CStandard { get; set; }
    public string CppStandard { get; set; }
    public IReadOnlyList<string> AdditionalArguments { get; set; }
}
```

服务接口：

```csharp
public interface IFileDiscoveryService
{
    Task<IReadOnlyList<ScanInputFile>> DiscoverAsync(CodeCheckConfig config, CancellationToken cancellationToken);
}

public interface ICompileContextBuilder
{
    Task<CompileContext> BuildAsync(CodeCheckConfig config, IReadOnlyList<ScanInputFile> files, CancellationToken cancellationToken);
}
```

### 26.7 第三方引擎接口

统一引擎接口：

```csharp
public interface IAnalysisEngine
{
    string Name { get; }

    Task<EngineResult> AnalyzeAsync(
        ScanContext context,
        IReadOnlyList<ScanInputFile> files,
        CancellationToken cancellationToken);
}
```

第一版引擎实现：

```text
ClangTidyRunner
CppcheckRunner
LizardRunner
BuiltinRuleRunner
```

结果模型：

```csharp
public sealed class EngineResult
{
    public string EngineName { get; set; }
    public IReadOnlyList<Issue> Issues { get; set; }
    public IReadOnlyList<FailedFile> FailedFiles { get; set; }
    public MetricSummary Metrics { get; set; }
}
```

### 26.8 基线、误报、评分接口

建议接口：

```csharp
public interface IFingerprintService
{
    void ApplyFingerprints(IReadOnlyList<Issue> issues);
}

public interface IBaselineService
{
    Task<BaselineCompareResult> CompareAsync(IReadOnlyList<Issue> issues, BaselineConfig config, CancellationToken cancellationToken);
    Task CreateOrUpdateAsync(IReadOnlyList<Issue> issues, BaselineConfig config, CancellationToken cancellationToken);
}

public interface ISuppressionService
{
    Task ApplyAsync(IReadOnlyList<Issue> issues, SuppressionConfig config, CancellationToken cancellationToken);
}

public interface IQualityScoreService
{
    QualityScore Calculate(IReadOnlyList<Issue> issues, ReportConfig reportConfig);
}
```

### 26.9 `CodeCheck.Cli` 模块结构

建议目录：

```text
CodeCheck.Cli
├── Commands
│   ├── ScanCommand.cs
│   ├── ValidateCommand.cs
│   ├── BaselineCommand.cs
│   ├── SuppressCommand.cs
│   └── ExportCommand.cs
├── Services
│   ├── ScanOrchestrator.cs
│   ├── CliProgressReporter.cs
│   └── ControlFileService.cs
└── Program.cs
```

核心调度接口：

```csharp
public interface IScanOrchestrator
{
    Task<ScanReport> RunAsync(string configPath, CommandLineOverrides overrides, CancellationToken cancellationToken);
}
```

`ScanOrchestrator` 负责串联：

```text
配置加载 -> 配置校验 -> 规则加载 -> 文件发现 -> 编译上下文 -> 引擎执行 -> 结果归一化 -> 指纹 -> 抑制 -> 基线 -> 评分 -> 报告生成
```

### 26.10 `CodeCheck.Reporting` 模块结构

建议目录：

```text
CodeCheck.Reporting
├── Json
│   └── ReportJsonWriter.cs
├── Html
│   └── HtmlReportWriter.cs
├── Sarif
│   └── SarifReportWriter.cs
├── Xlsx
│   └── XlsxReportWriter.cs
└── Templates
```

统一接口：

```csharp
public interface IReportWriter
{
    string Format { get; }

    Task WriteAsync(ScanReport report, string outputPath, CancellationToken cancellationToken);
}
```

第一版实现：

```text
ReportJsonWriter
HtmlReportWriter
SarifReportWriter
XlsxReportWriter
```

### 26.11 `CodeCheck.Desktop` 模块结构

建议目录：

```text
CodeCheck.Desktop
├── Views
├── ViewModels
├── Services
├── Models
├── Commands
└── App.xaml
```

主要 View：

```text
HomeView
NewScanView
ScanConfigView
ScanProgressView
ResultView
ReportPreviewView
RuleConfigView
BaselineView
SuppressionView
SettingsView
```

主要服务：

```text
CliRunnerService
ConfigService
ReportService
RuleService
BaselineService
SuppressionService
FileDialogService
ToolHealthCheckService
WebViewReportService
```

GUI 不直接执行静态分析，只负责：

```text
生成配置 -> 调用 CLI -> 解析进度 -> 加载报告 -> 修改基线/误报配置
```

### 26.12 测试项目结构

建议目录：

```text
CodeCheck.Tests
├── Configuration
├── Rules
├── Input
├── Baseline
├── Suppression
├── Reporting
├── Engines
└── TestData
```

优先测试：

- 配置解析；
- 规则加载；
- 文件发现；
- 排除规则；
- 指纹生成；
- 基线对比；
- 误报匹配；
- 质量评分；
- SARIF 生成；
- Lizard 输出解析。

---

## 27. 下一步待继续设计

后续建议继续设计：

1. `rules` 目录下 100 条规则清单初稿；
2. `default-codecheck.json` 完整默认配置；
3. `report.json` 完整示例；
4. WPF 页面原型和 ViewModel 字段；
5. 第三方工具获取、版本固定和 release 集成脚本；
6. 样例工程和验收测试用例。

---

## 28. `CodeCheck.Cli` 详细扫描时序和异常处理

本节定义 `CodeCheck.Cli.exe scan` 的详细执行流程、状态流转、异常分类、失败文件处理、退出码和日志策略。

### 28.1 CLI 扫描总目标

`scan` 命令需要做到：

```text
配置错误时明确失败；
工具缺失时明确失败；
单个文件扫描失败时不中断整体任务；
单个引擎失败时不中断其他引擎；
用户取消时保存已有结果；
所有失败都能在 report.json 和 scan.log 中追踪。
```

### 28.2 扫描主时序

`ScanOrchestrator.RunAsync` 建议按以下顺序执行：

```text
01. 初始化 ScanRunContext
02. 解析命令行参数
03. 加载 codecheck.json
04. 合并命令行覆盖项
05. 解析 release 根目录和相对路径
06. 校验配置结构
07. 校验工具依赖
08. 校验输出目录、日志目录、temp 目录权限
09. 加载 rules.index.json
10. 加载规则文件和 rule-mapping.json
11. 应用 rule-profiles.json 中的 default profile
12. 应用 disabledRules 和 severityOverrides
13. 加载 suppression.json，不存在则创建空结构
14. 检查 baseline.json 是否存在
15. 发现输入文件
16. 应用默认排除目录、排除文件、排除 pattern
17. 识别语言类型和头文件策略
18. 解析 .vcxproj/.pro
19. 构建 CompileContext
20. 校验 CompileContext，不允许降级扫描
21. 构建扫描任务队列
22. 执行 clang-tidy
23. 执行 cppcheck
24. 执行 lizard
25. 执行 builtin 规则
26. 归一化所有引擎结果为 Issue
27. 合并重复问题
28. 生成 issueId 和 fingerprint
29. 应用 suppression
30. 应用 baseline 对比，或首次自动创建 baseline
31. 计算 qualityScore
32. 汇总 metrics
33. 汇总 failedFiles、disabledRules、logs
34. 生成 report.json
35. 生成 report.html
36. 生成 report.sarif
37. 如果 generateXlsx=true，生成 report.xlsx
38. 输出 scan-completed JSON Lines 事件
39. 返回退出码
```

### 28.3 扫描状态定义

`scan.status` 建议取值：

| 状态 | 含义 |
|---|---|
| `NotStarted` | 尚未启动 |
| `Validating` | 正在校验配置和依赖 |
| `DiscoveringFiles` | 正在发现文件 |
| `BuildingCompileContext` | 正在构建编译上下文 |
| `RunningEngines` | 正在执行检查引擎 |
| `PostProcessing` | 正在后处理、抑制、基线、评分 |
| `GeneratingReports` | 正在生成报告 |
| `Completed` | 扫描完成且无扫描失败文件 |
| `CompletedWithFailedFiles` | 扫描完成但存在失败文件 |
| `Cancelled` | 用户取消 |
| `ConfigError` | 配置错误导致无法扫描 |
| `DependencyError` | 工具依赖缺失导致无法扫描 |
| `Failed` | 未处理异常或关键流程失败 |

最终报告中只建议出现：

```text
Completed
CompletedWithFailedFiles
Cancelled
ConfigError
DependencyError
Failed
```

### 28.4 JSON Lines 进度事件

CLI 标准输出使用 JSON Lines，便于 GUI 实时解析。

建议事件：

```json
{"type":"scan-started","time":"2026-01-01T10:00:00","project":"DemoProject"}
{"type":"stage-started","stage":"Validating"}
{"type":"stage-completed","stage":"Validating"}
{"type":"files-discovered","total":230,"scheduled":128,"excluded":102}
{"type":"engine-started","engine":"lizard","fileCount":128}
{"type":"file-started","engine":"clang-tidy","current":1,"total":128,"file":"src\\main.cpp"}
{"type":"issue-found","severity":"Warning","ruleId":"Quectel-CPP-038","file":"src\\control.cpp","line":128}
{"type":"file-failed","engine":"clang-tidy","file":"src\\main.cpp","reason":"MissingInclude"}
{"type":"engine-completed","engine":"lizard","issues":5,"failedFiles":0}
{"type":"baseline-created","path":"baseline\\DemoProject_8F31A2.baseline.json","issueCount":38}
{"type":"report-generated","format":"html","path":"reports\\DemoProject\\20260101_100000\\report.html"}
{"type":"scan-completed","status":"CompletedWithFailedFiles","totalIssues":38,"report":"reports\\DemoProject\\20260101_100000\\report.json"}
```

错误事件：

```json
{"type":"error","stage":"ConfigValidation","code":"MissingIncludeDirectories","message":"缺少 include 路径，且不允许降级扫描。"}
```

### 28.5 异常分类

异常建议按以下类别处理：

| 异常类型 | 是否中断扫描 | 处理方式 |
|---|---|---|
| 配置文件不存在 | 是 | `ConfigError` |
| 配置 JSON 格式错误 | 是 | `ConfigError` |
| 规则库不存在 | 是 | `ConfigError` 或 `DependencyError` |
| 第三方工具 exe 不存在 | 是 | `DependencyError` |
| 输出目录不可写 | 是 | `ConfigError` |
| include 路径缺失 | 是 | `ConfigError` |
| `.vcxproj/.pro` 解析失败 | 否/视情况 | 记录 warning，若无可用 include 则 `ConfigError` |
| 单文件 clang-tidy 执行失败 | 否 | 记录 `failedFiles` |
| 单文件 cppcheck 执行失败 | 否 | 记录 `failedFiles` |
| lizard 执行失败 | 否/视情况 | 记录 `failedFiles` 或 engine warning |
| report.html 生成失败 | 是 | `Failed`，但保留 `report.json` |
| report.sarif 生成失败 | 否 | 记录 warning，扫描状态可为 `CompletedWithFailedFiles` |
| 用户取消 | 是 | `Cancelled`，保存部分报告 |
| 未处理异常 | 是 | `Failed` |

### 28.6 配置错误处理策略

以下错误应在扫描前阻断：

```text
codecheck.json 不存在；
codecheck.json 无法解析；
input.paths 和 fileList 都为空；
输入路径不存在；
rules.index.json 不存在；
启用的引擎路径不存在；
requireCompileContext=true 但无法获得 include 路径；
输出目录不可写；
关闭规则缺少 reason；
关闭 Blocker/Critical 规则但 riskAccepted=false。
```

阻断时：

```text
不执行第三方工具；
生成最小 report.json；
scan.status = ConfigError 或 DependencyError；
退出码不为 0；
GUI 展示中文错误。
```

### 28.7 单文件失败处理

如果某个文件在某个引擎下失败，不应中断整体扫描。

示例：

```json
{
  "file": "D:\\项目代码\\DemoProject\\src\\main.cpp",
  "relativeFile": "src\\main.cpp",
  "language": "cpp",
  "engine": "clang-tidy",
  "stage": "Parse",
  "status": "Failed",
  "errorCode": "MissingInclude",
  "message": "无法找到头文件 QtCore/QObject。",
  "details": "fatal error: 'QtCore/QObject' file not found",
  "suggestion": "请补充 Qt include 路径，或从 .pro/.vcxproj 读取工程配置。"
}
```

`failedFiles` 的 `stage` 建议取值：

```text
InputDiscovery
ConfigValidation
ProjectParse
Preprocess
Parse
EngineRun
ResultParse
ReportGeneration
```

### 28.8 第三方引擎执行策略

第一版建议执行顺序：

```text
lizard -> cppcheck -> clang-tidy -> builtin
```

原因：

- `lizard` 对编译上下文依赖较低，适合快速产生复杂度结果；
- `cppcheck` 相对轻量；
- `clang-tidy` 对 include 和宏更敏感，耗时更长；
- `builtin` 可以在最后补充文本类规则，也可以按规则情况提前执行。

如果希望用户尽早看到问题，也可以并行执行不同引擎，但第一版建议先串行或有限并发，降低复杂度。

### 28.9 并发策略

建议第一版采用文件级并发，但限制最大并发数：

```json
"runtime": {
  "maxParallelism": 4
}
```

建议：

- `lizard` 可批量调用；
- `cppcheck` 可按文件或批量调用；
- `clang-tidy` 建议按文件调用，便于失败定位；
- 同时运行的外部进程数量不超过 `maxParallelism`。

### 28.10 暂停、继续、取消细节

控制文件：

```text
temp\.codecheck-control.json
```

内容：

```json
{ "command": "pause" }
```

处理策略：

- 每启动一个新文件任务前检查控制文件；
- 每个引擎批次执行前检查控制文件；
- `pause`：不再启动新任务，等待 `resume` 或 `cancel`；
- `resume`：继续任务队列；
- `cancel`：停止任务队列，尝试结束当前外部进程；
- 取消后仍生成部分 `report.json` 和 `scan.log`。

取消后的报告：

```json
"scan": {
  "status": "Cancelled",
  "cancelled": true
}
```

### 28.11 退出码设计

`CodeCheck.Cli.exe` 退出码建议：

| 退出码 | 含义 |
|---:|---|
| `0` | 扫描成功，工具执行无错误 |
| `1` | 扫描完成，但存在 `Blocker` 或 `Critical` 问题 |
| `2` | 配置错误 |
| `3` | 工具依赖缺失 |
| `4` | 规则库加载失败 |
| `5` | 未处理异常 |
| `6` | 用户取消扫描 |
| `7` | 扫描完成，但存在扫描失败文件 |

说明：

```text
发现代码问题不等于工具执行失败；
GUI 应优先读取 report.json 判断扫描结果，而不是只依赖退出码。
```

### 28.12 日志策略

每次扫描生成：

```text
reports\{ProjectName}\{Timestamp}\scan.log
```

日志级别：

```text
Debug
Info
Warning
Error
```

日志内容：

- CLI 启动参数；
- 配置文件路径；
- release 根目录；
- 工具版本；
- 规则库版本；
- 输入文件数量；
- 排除文件数量；
- include 路径和宏定义摘要；
- 每个引擎启动和结束；
- 失败文件详情；
- 报告输出路径；
- 未处理异常堆栈。

`report.json` 中只保留日志摘要，完整日志写入 `scan.log`。

### 28.13 最小失败报告

如果扫描在配置阶段失败，也应生成最小 `report.json`，便于 GUI 展示。

最小报告包含：

```json
{
  "schemaVersion": "1.0.0",
  "tool": {},
  "project": {},
  "scan": {
    "status": "ConfigError"
  },
  "summary": {
    "totalIssues": 0
  },
  "failedFiles": [],
  "logs": [
    {
      "level": "Error",
      "message": "缺少 include 路径，且不允许降级扫描。"
    }
  ]
}
```

### 28.14 扫描完成判定

扫描结束时按以下优先级确定最终状态：

```text
用户取消                         -> Cancelled
配置错误                         -> ConfigError
依赖缺失                         -> DependencyError
未处理异常                       -> Failed
存在 failedFiles                 -> CompletedWithFailedFiles
无 failedFiles 且流程完成         -> Completed
```

退出码再根据最终状态和问题级别计算。

### 28.15 下一步待继续设计

后续建议继续设计：

1. `default-codecheck.json` 完整默认配置；
2. `report.json` 完整示例；
3. WPF 页面原型和 ViewModel 字段；
4. 第三方工具获取、版本固定和 release 集成脚本；
5. 样例工程和验收测试用例。

---

## 29. 第一版 100 条规则清单初稿

本节给出第一版 `100` 条规则清单初稿，用于后续生成 `rules/*.json`。

说明：

- 该清单以当前讨论结果为基础；
- 华为规范类规则需在后续完整解析 `C++Languagelawer.pdf` 后再校准名称、分类和示例；
- `CERT` 规则以第一版低误报、易定位、易修复为优先原则选取；
- 若华为规范与 `CERT` 冲突，以 `CERT` 为准；
- 每条规则后续都需要补充中文错误示例、正确示例和修复建议；
- 默认规则集为 `default`，普通用户不需要选择规则集。

### 29.1 规则数量分布

| 来源 | 数量 |
|---|---:|
| 华为 C 规范 | 22 |
| 华为 C++ 规范 | 38 |
| CERT C | 20 |
| CERT C++ | 20 |
| 合计 | 100 |

### 29.2 检测引擎说明

| 检测方式 | 含义 |
|---|---|
| `clang-tidy` | 由 `clang-tidy` 检测 |
| `cppcheck` | 由 `cppcheck` 检测 |
| `lizard` | 由 `lizard` 统计度量后生成问题 |
| `builtin` | 由 `CodeCheckBuiltin` 内置规则检测 |
| `combined` | 多个引擎共同检测 |
| `manual` | 第一版不可自动检测，仅作为人工评审建议保留 |

### 29.3 华为 C 规范规则，22 条

| 编号 | 规则名称 | 级别 | 检测方式 | 默认启用 |
|---|---|---|---|---|
| `Quectel-C-001` | 禁止使用 `gets` 等无边界输入函数 | `Blocker` | `builtin` | 是 |
| `Quectel-C-002` | 禁止使用不限制长度的字符串拷贝函数 | `Critical` | `builtin`/`cppcheck` | 是 |
| `Quectel-C-003` | 禁止使用不限制长度的格式化输出函数 | `Critical` | `builtin` | 是 |
| `Quectel-C-004` | 使用数组时必须保证下标边界有效 | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-C-005` | 指针使用前必须判空或保证有效 | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-C-006` | 局部变量必须初始化后再使用 | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-C-007` | 动态内存申请结果必须检查 | `Critical` | `builtin`/`cppcheck` | 是 |
| `Quectel-C-008` | 动态申请的内存必须在所有路径释放 | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-C-009` | 禁止重复释放同一内存资源 | `Blocker` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-C-010` | 禁止释放后继续访问内存 | `Blocker` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-C-011` | 文件、句柄、锁等资源申请后必须释放 | `Critical` | `cppcheck`/`builtin` | 是 |
| `Quectel-C-012` | 函数返回值应表达明确错误状态 | `Warning` | `manual` | 否 |
| `Quectel-C-013` | 禁止忽略关键库函数返回值 | `Warning` | `builtin`/`clang-tidy` | 是 |
| `Quectel-C-014` | `switch` 语句应包含 `default` 分支 | `Warning` | `clang-tidy`/`builtin` | 是 |
| `Quectel-C-015` | `case` 分支需要明确 `break` 或注释说明贯穿 | `Warning` | `clang-tidy`/`builtin` | 是 |
| `Quectel-C-016` | 宏定义应使用全大写和下划线命名 | `Suggestion` | `builtin` | 是 |
| `Quectel-C-017` | 宏参数和宏整体表达式应使用括号保护 | `Warning` | `builtin`/`cppcheck` | 是 |
| `Quectel-C-018` | 禁止在头文件中定义可导致重复定义的全局对象 | `Warning` | `builtin` | 是 |
| `Quectel-C-019` | 函数参数数量不宜过多 | `Suggestion` | `lizard`/`builtin` | 是 |
| `Quectel-C-020` | 函数嵌套层级不宜过深 | `Warning` | `clang-tidy`/`builtin` | 是 |
| `Quectel-C-021` | 函数圈复杂度不得超过 10 | `Warning` | `lizard` | 是 |
| `Quectel-C-022` | 函数体代码行数不得超过 100 行 | `Warning` | `lizard` | 是 |

### 29.4 华为 C++ 规范规则，38 条

| 编号 | 规则名称 | 级别 | 检测方式 | 默认启用 |
|---|---|---|---|---|
| `Quectel-CPP-001` | 头文件中禁止使用 `using namespace` | `Warning` | `builtin` | 是 |
| `Quectel-CPP-002` | 头文件中禁止定义非 `inline` 普通函数 | `Warning` | `builtin` | 是 |
| `Quectel-CPP-003` | 头文件应具有防重复包含保护 | `Warning` | `builtin` | 是 |
| `Quectel-CPP-004` | 禁止在头文件中定义非必要全局变量 | `Warning` | `builtin` | 是 |
| `Quectel-CPP-005` | 类存在虚函数时析构函数应为 `virtual` | `Critical` | `clang-tidy` | 是 |
| `Quectel-CPP-006` | 禁止通过非虚析构基类指针删除派生对象 | `Critical` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CPP-007` | 重写虚函数应使用 `override` | `Suggestion` | `clang-tidy` | 是 |
| `Quectel-CPP-008` | 禁止在构造和析构函数中调用虚函数 | `Critical` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CPP-009` | 拥有资源的类应遵循拷贝控制规则 | `Critical` | `clang-tidy` | 是 |
| `Quectel-CPP-010` | 禁止裸 `new/delete` 管理资源，优先使用 RAII | `Warning` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CPP-011` | 禁止内存申请后未释放 | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CPP-012` | 禁止释放后继续访问对象 | `Blocker` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CPP-013` | 禁止重复释放对象或资源 | `Blocker` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CPP-014` | 指针解引用前必须保证有效 | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CPP-015` | 局部对象和变量应初始化后使用 | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CPP-016` | 避免 C 风格强制类型转换 | `Warning` | `clang-tidy` | 是 |
| `Quectel-CPP-017` | 避免不安全的 `reinterpret_cast` | `Critical` | `clang-tidy` | 是 |
| `Quectel-CPP-018` | 优先使用 `nullptr`，不使用 `NULL` 或 `0` 表示空指针 | `Suggestion` | `clang-tidy` | 是 |
| `Quectel-CPP-019` | 可不修改对象状态的成员函数应声明为 `const` | `Suggestion` | `clang-tidy` | 是 |
| `Quectel-CPP-020` | 可不修改的变量和参数应使用 `const` | `Suggestion` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CPP-021` | 禁止忽略关键函数返回值 | `Warning` | `builtin`/`clang-tidy` | 是 |
| `Quectel-CPP-022` | `switch` 语句应包含 `default` 分支 | `Warning` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CPP-023` | `case` 分支需要明确 `break` 或注释说明贯穿 | `Warning` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CPP-024` | 禁止捕获异常后完全忽略 | `Warning` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CPP-025` | 析构函数不应抛出异常 | `Critical` | `clang-tidy` | 是 |
| `Quectel-CPP-026` | 禁止使用已弃用或危险 C 库函数 | `Critical` | `builtin` | 是 |
| `Quectel-CPP-027` | 宏定义应避免替代类型安全的常量或函数 | `Suggestion` | `builtin`/`clang-tidy` | 是 |
| `Quectel-CPP-028` | 枚举、类、函数、变量命名应符合统一风格 | `Suggestion` | `builtin` | 是 |
| `Quectel-CPP-029` | 函数参数数量不宜过多 | `Suggestion` | `lizard`/`builtin` | 是 |
| `Quectel-CPP-030` | 函数嵌套层级不宜过深 | `Warning` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CPP-031` | 禁止大段注释掉的无效代码 | `Suggestion` | `builtin` | 是 |
| `Quectel-CPP-032` | 单行代码长度不宜过长 | `Suggestion` | `builtin` | 是 |
| `Quectel-CPP-033` | 包含顺序应稳定，避免无用头文件 | `Suggestion` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CPP-034` | 禁止循环中进行明显低效的重复计算 | `Suggestion` | `clang-tidy` | 是 |
| `Quectel-CPP-035` | 禁止返回局部对象的指针或引用 | `Blocker` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CPP-036` | 禁止对象切片导致多态信息丢失 | `Warning` | `clang-tidy` | 是 |
| `Quectel-CPP-037` | 函数圈复杂度不得超过 10 | `Warning` | `lizard` | 是 |
| `Quectel-CPP-038` | 函数体代码行数不得超过 100 行 | `Warning` | `lizard` | 是 |

### 29.5 CERT C 规则，20 条

| 编号 | 规则名称 | CERT 参考 | 级别 | 检测方式 | 默认启用 |
|---|---|---|---|---|---|
| `Quectel-CERT-C-001` | 字符串转换整数时必须检查转换错误 | `ERR34-C` | `Critical` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CERT-C-002` | 不应忽略错误指示或异常返回值 | `ERR33-C` | `Warning` | `builtin`/`clang-tidy` | 是 |
| `Quectel-CERT-C-003` | 禁止读取未初始化对象 | `EXP33-C` | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CERT-C-004` | 不得越界访问数组 | `ARR30-C` | `Blocker` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CERT-C-005` | 字符串必须以空字符正确终止 | `STR32-C` | `Critical` | `cppcheck`/`builtin` | 是 |
| `Quectel-CERT-C-006` | 字符串拷贝必须保证目标空间足够 | `STR31-C` | `Critical` | `cppcheck`/`builtin` | 是 |
| `Quectel-CERT-C-007` | 禁止使用已释放内存 | `MEM30-C` | `Blocker` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CERT-C-008` | 动态分配的内存只释放一次 | `MEM31-C`/`MEM34-C` | `Blocker` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CERT-C-009` | 动态内存申请结果必须检查 | `MEM32-C` | `Critical` | `builtin`/`cppcheck` | 是 |
| `Quectel-CERT-C-010` | 禁止对无效指针执行指针运算 | `ARR36-C` | `Critical` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-C-011` | 禁止除零和取模零 | `INT33-C` | `Blocker` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-C-012` | 整数转换不得导致截断或符号错误 | `INT31-C` | `Warning` | `clang-tidy` | 是 |
| `Quectel-CERT-C-013` | 整数运算应避免溢出风险 | `INT32-C` | `Warning` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-C-014` | 格式化输入输出参数类型必须匹配 | `FIO47-C` | `Critical` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-C-015` | 禁止使用不受信任的格式化字符串 | `FIO30-C` | `Critical` | `clang-tidy`/`builtin` | 是 |
| `Quectel-CERT-C-016` | 文件操作后必须检查错误状态 | `FIO` | `Warning` | `builtin`/`cppcheck` | 是 |
| `Quectel-CERT-C-017` | 信号处理函数中只能调用异步信号安全函数 | `SIG30-C` | `Warning` | `manual` | 否 |
| `Quectel-CERT-C-018` | 多线程共享数据必须受同步保护 | `CON` | `Warning` | `manual` | 否 |
| `Quectel-CERT-C-019` | 禁止使用危险临时文件创建方式 | `FIO21-C` | `Warning` | `builtin` | 是 |
| `Quectel-CERT-C-020` | 禁止返回局部对象地址 | `DCL30-C` | `Blocker` | `clang-tidy`/`cppcheck` | 是 |

### 29.6 CERT C++ 规则，20 条

| 编号 | 规则名称 | CERT 参考 | 级别 | 检测方式 | 默认启用 |
|---|---|---|---|---|---|
| `Quectel-CERT-CPP-001` | 基类析构函数应支持多态删除 | `OOP52-CPP` | `Critical` | `clang-tidy` | 是 |
| `Quectel-CERT-CPP-002` | 拷贝构造和拷贝赋值应正确处理自赋值 | `OOP54-CPP` | `Warning` | `clang-tidy` | 是 |
| `Quectel-CERT-CPP-003` | 析构函数不得抛出异常 | `DCL57-CPP` | `Critical` | `clang-tidy` | 是 |
| `Quectel-CERT-CPP-004` | 构造函数中资源获取失败应保持对象状态安全 | `ERR` | `Warning` | `manual` | 否 |
| `Quectel-CERT-CPP-005` | 禁止访问生命周期已结束的对象 | `MEM50-CPP` | `Blocker` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-CPP-006` | 动态分配内存必须正确释放 | `MEM51-CPP` | `Critical` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-CPP-007` | 禁止使用不匹配的分配和释放形式 | `MEM51-CPP` | `Critical` | `cppcheck`/`clang-tidy` | 是 |
| `Quectel-CERT-CPP-008` | 禁止空指针解引用 | `EXP34-C` | `Blocker` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-CPP-009` | 表达式求值不应依赖未指定顺序 | `EXP50-CPP` | `Warning` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-CPP-010` | 不应忽略具有错误含义的返回值 | `ERR` | `Warning` | `builtin`/`clang-tidy` | 是 |
| `Quectel-CERT-CPP-011` | 异常处理不应捕获后完全忽略 | `ERR` | `Warning` | `builtin`/`clang-tidy` | 是 |
| `Quectel-CERT-CPP-012` | 禁止从析构函数中逃逸异常 | `ERR` | `Critical` | `clang-tidy` | 是 |
| `Quectel-CERT-CPP-013` | 禁止返回局部对象的引用或指针 | `DCL30-C` | `Blocker` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-CPP-014` | 禁止对象切片破坏多态语义 | `OOP51-CPP` | `Warning` | `clang-tidy` | 是 |
| `Quectel-CERT-CPP-015` | 禁止越界访问容器或数组 | `CTR50-CPP` | `Critical` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-CPP-016` | 迭代器使用前必须保证有效 | `CTR51-CPP` | `Critical` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-CPP-017` | 禁止对无效迭代器执行解引用 | `CTR55-CPP` | `Critical` | `clang-tidy`/`cppcheck` | 是 |
| `Quectel-CERT-CPP-018` | 类型转换不得破坏对象有效类型 | `EXP` | `Critical` | `clang-tidy` | 是 |
| `Quectel-CERT-CPP-019` | 多线程共享对象访问必须同步 | `CON` | `Warning` | `manual` | 否 |
| `Quectel-CERT-CPP-020` | 禁止使用危险或过时的库接口 | `MSC` | `Warning` | `builtin`/`clang-tidy` | 是 |

### 29.7 规则落地文件划分

后续生成规则库文件时，建议按以下方式拆分：

```text
rules\quectel-c-rules.json        -> Quectel-C-001 ~ Quectel-C-022
rules\quectel-cpp-rules.json      -> Quectel-CPP-001 ~ Quectel-CPP-038
rules\cert-c-rules.json           -> Quectel-CERT-C-001 ~ Quectel-CERT-C-020
rules\cert-cpp-rules.json         -> Quectel-CERT-CPP-001 ~ Quectel-CERT-CPP-020
rules\rule-mapping.json           -> CodeCheck 规则到 clang-tidy/cppcheck/lizard/builtin 的映射
rules\rule-profiles.json          -> default、strict、cert-only 规则集
```

### 29.8 下一步待继续设计

后续建议继续设计：

1. `report.json` 完整示例；
2. WPF 页面原型和 ViewModel 字段；
3. 第三方工具获取、版本固定和 release 集成脚本；
4. 样例工程和验收测试用例。

---

## 30. `default-codecheck.json` 完整默认配置

本节定义第一版默认配置文件 `configs/default-codecheck.json`。

该文件作为 GUI 新建扫描时的模板，普通用户一般不直接修改。GUI 会根据用户选择的目录、文件、include 路径、宏定义、Qt 路径等信息生成临时配置文件：

```text
temp\current-codecheck.json
```

### 30.1 默认配置设计原则

默认配置遵循以下原则：

```text
默认使用 default 规则集；
默认启用 clang-tidy、cppcheck、lizard、builtin；
默认不扫描第三方目录；
默认排除 Qt 生成文件；
默认 C 标准为 c11；
默认 C++ 标准为 c++14；
默认不允许降级扫描；
默认启用 baseline；
默认首次扫描自动创建 baseline；
默认启用 suppression；
默认生成 HTML、SARIF、JSON；
默认不生成 XLSX，用户选择导出时再生成；
默认质量评分为 100 分制；
默认最大并发为 4。
```

### 30.2 完整默认配置

```json
{
  "version": "1.0.0",
  "project": {
    "name": "",
    "root": "",
    "description": "",
    "owner": "",
    "projectKey": ""
  },
  "input": {
    "type": "directory",
    "paths": [],
    "fileList": "",
    "sourceExtensions": [
      ".c",
      ".cc",
      ".cpp",
      ".cxx"
    ],
    "headerExtensions": [
      ".h",
      ".hh",
      ".hpp",
      ".hxx"
    ],
    "headerScanPolicy": {
      "scanHeadersInDirectoryMode": false,
      "allowHeaderAsExplicitInput": true,
      "headerLanguageMode": "auto"
    }
  },
  "build": {
    "languageStandard": {
      "c": "c11",
      "cpp": "c++14"
    },
    "loadFromProjectFile": true,
    "autoDetectProjectFiles": true,
    "projectFiles": [],
    "supportedProjectTypes": [
      "vcxproj",
      "pro"
    ],
    "includeDirectories": [],
    "defines": [],
    "additionalArguments": [],
    "requireCompileContext": true,
    "allowDegradedScan": false,
    "msvc": {
      "enabled": true,
      "autoDetect": true,
      "visualStudioRoot": "",
      "windowsSdkRoot": "",
      "additionalIncludeDirectories": []
    },
    "qt": {
      "enabled": true,
      "autoDetect": false,
      "root": "",
      "modules": [
        "Core",
        "Gui",
        "Widgets"
      ],
      "additionalIncludeDirectories": []
    }
  },
  "scan": {
    "excludeDirectories": [
      "third_party",
      "3rdparty",
      "external",
      "extern",
      "vendor",
      "vendors",
      "dependencies",
      "deps",
      "packages",
      "build",
      "out",
      "bin",
      "obj",
      "Debug",
      "Release",
      "x64",
      "x86",
      ".git",
      ".svn",
      ".vs"
    ],
    "excludeFiles": [
      "moc_*.cpp",
      "ui_*.h",
      "qrc_*.cpp",
      "mocs_compilation.cpp"
    ],
    "excludePatterns": [
      "**/*_autogen/**",
      "**/GeneratedFiles/**"
    ],
    "followSymbolicLinks": false,
    "explicitInputOverridesExcludes": true,
    "scanFailedFilesVisible": true,
    "collectCodeSnippet": true,
    "codeSnippetContextLines": 2
  },
  "engines": {
    "clangTidy": {
      "enabled": true,
      "path": "tools\\llvm\\bin\\clang-tidy.exe",
      "checks": [
        "clang-analyzer-*",
        "bugprone-*",
        "cert-*",
        "cppcoreguidelines-*",
        "performance-*",
        "readability-*"
      ],
      "disabledChecks": [
        "readability-magic-numbers",
        "cppcoreguidelines-avoid-magic-numbers"
      ],
      "warningsAsErrors": false,
      "extraArgs": [],
      "timeoutSecondsPerFile": 120
    },
    "cppcheck": {
      "enabled": true,
      "path": "tools\\cppcheck\\cppcheck.exe",
      "enable": [
        "warning",
        "style",
        "performance",
        "portability"
      ],
      "inconclusive": false,
      "xml": true,
      "xmlVersion": 2,
      "extraArgs": [],
      "timeoutSecondsPerFile": 120
    },
    "lizard": {
      "enabled": true,
      "path": "tools\\lizard\\lizard.exe",
      "thresholds": {
        "cyclomaticComplexity": 10,
        "functionLines": 100,
        "parameterCount": 8
      },
      "extraArgs": [],
      "timeoutSeconds": 300
    },
    "builtin": {
      "enabled": true,
      "rules": {
        "forbiddenApis": true,
        "headerUsingNamespace": true,
        "headerNonInlineFunction": true,
        "lineLength": true,
        "commentedOutCode": true,
        "macroStyle": true
      }
    }
  },
  "rules": {
    "ruleIndex": "rules\\rules.index.json",
    "profile": "default",
    "disabledRules": [],
    "severityOverrides": []
  },
  "baseline": {
    "enabled": true,
    "mode": "compare",
    "path": "",
    "createIfMissing": true,
    "onlyShowNewIssuesByDefault": false,
    "excludeSuppressedIssuesWhenCreating": true
  },
  "suppression": {
    "enabled": true,
    "path": "",
    "allowSourceCommentSuppression": false,
    "defaultScope": "single-issue",
    "requireReason": true
  },
  "report": {
    "outputDirectory": "reports",
    "formats": [
      "html",
      "sarif",
      "json"
    ],
    "optionalFormats": [
      "xlsx"
    ],
    "language": "zh-CN",
    "generateHtml": true,
    "generateSarif": true,
    "generateJson": true,
    "generateXlsx": false,
    "html": {
      "singleFile": true,
      "showCodeSnippet": true,
      "showDisabledRules": true,
      "showSuppressedIssues": true,
      "showFailedFiles": true,
      "showMetrics": true,
      "showTopComplexFunctions": true,
      "showTopLongFunctions": true
    },
    "sarif": {
      "version": "2.1.0"
    },
    "xlsx": {
      "includeSummary": true,
      "includeIssues": true,
      "includeSuppressedIssues": true,
      "includeFailedFiles": true,
      "includeDisabledRules": true
    },
    "qualityScore": {
      "enabled": true,
      "baseScore": 100,
      "minimumScore": 0,
      "deduction": {
        "Blocker": 10,
        "Critical": 5,
        "Warning": 2,
        "Suggestion": 0.5
      },
      "levels": [
        {
          "name": "优秀",
          "min": 90,
          "max": 100
        },
        {
          "name": "良好",
          "min": 80,
          "max": 89
        },
        {
          "name": "一般",
          "min": 70,
          "max": 79
        },
        {
          "name": "较差",
          "min": 60,
          "max": 69
        },
        {
          "name": "高风险",
          "min": 0,
          "max": 59
        }
      ],
      "excludeSuppressedIssues": true
    }
  },
  "runtime": {
    "maxParallelism": 4,
    "enablePause": true,
    "enableCancel": true,
    "controlFile": "temp\\.codecheck-control.json",
    "logLevel": "Info",
    "logDirectory": "logs",
    "tempDirectory": "temp",
    "consoleOutput": "json-lines",
    "pathEncoding": "utf-8"
  }
}
```

### 30.3 关键字段说明

#### 30.3.1 `project`

`project` 由 GUI 在用户选择扫描对象后自动填充。

建议填充规则：

```text
扫描目录：project.name 使用目录名；project.root 使用目录路径；
扫描单文件：project.name 使用文件名去扩展名；project.root 使用文件所在目录；
扫描多文件：project.name 使用 MultiFileScan 或用户输入名称；project.root 使用公共父目录；
projectKey 使用 project.name + hash(project.root 或 fileList)。
```

#### 30.3.2 `input.type`

支持：

```text
directory
file
file-list
```

含义：

| 值 | 含义 |
|---|---|
| `directory` | 扫描一个或多个目录 |
| `file` | 扫描单个文件，支持 `.h/.hpp/.hh/.hxx` |
| `file-list` | 扫描文件清单中的多个文件 |

#### 30.3.3 `headerScanPolicy`

默认：

```json
{
  "scanHeadersInDirectoryMode": false,
  "allowHeaderAsExplicitInput": true,
  "headerLanguageMode": "auto"
}
```

含义：

- 目录扫描默认不把头文件作为入口；
- 用户显式选择头文件时允许扫描；
- `.h` 文件可自动判断，也允许 GUI 中强制选择 C 或 C++14。

#### 30.3.4 `build.requireCompileContext`

默认：

```json
"requireCompileContext": true,
"allowDegradedScan": false
```

含义：

```text
没有 include 路径、宏定义或工程上下文时，不允许静默降级扫描。
```

如果无法构建编译上下文，应产生 `ConfigError`。

#### 30.3.5 `engines.clangTidy.checks`

默认启用较大的规则族，但最终实际启用规则要受 `rules/rule-mapping.json` 和 `rule-profiles.json` 控制。

默认禁用：

```text
readability-magic-numbers
cppcoreguidelines-avoid-magic-numbers
```

原因：

```text
魔法数字类规则第一版误报和争议较多，暂不作为默认强规则。
```

#### 30.3.6 `engines.lizard.thresholds`

默认：

```json
{
  "cyclomaticComplexity": 10,
  "functionLines": 100,
  "parameterCount": 8
}
```

第一版明确生成问题的是：

- 圈复杂度 `> 10`；
- 函数体代码行数 `> 100`。

`parameterCount` 第一版可仅用于 metrics 或建议类规则。

#### 30.3.7 `baseline.path` 和 `suppression.path`

默认为空，由 GUI 或 CLI 根据 `projectKey` 自动生成：

```text
baseline\{projectName}_{projectHash}.baseline.json
suppressions\{projectName}_{projectHash}.suppressions.json
```

#### 30.3.8 `report.outputDirectory`

默认：

```text
reports
```

实际扫描时生成：

```text
reports\{ProjectName}\{Timestamp}\report.json
reports\{ProjectName}\{Timestamp}\report.html
reports\{ProjectName}\{Timestamp}\report.sarif
reports\{ProjectName}\{Timestamp}\scan.log
```

如果用户选择导出 Excel，再生成：

```text
reports\{ProjectName}\{Timestamp}\report.xlsx
```

#### 30.3.9 `runtime.consoleOutput`

默认：

```json
"consoleOutput": "json-lines"
```

用于 GUI 实时解析扫描进度。

### 30.4 GUI 生成临时配置时的覆盖规则

GUI 基于 `configs/default-codecheck.json` 生成 `temp/current-codecheck.json` 时，需要覆盖以下字段：

```text
project.name
project.root
project.projectKey
input.type
input.paths
input.fileList
build.projectFiles
build.includeDirectories
build.defines
build.qt.root
baseline.path
suppression.path
report.outputDirectory
runtime.maxParallelism
```

普通用户流程中不覆盖：

```text
rules.profile
rules.disabledRules
rules.severityOverrides
```

除非用户进入“高级规则配置”。

### 30.5 `validate` 对默认配置的校验要求

`CodeCheck.Cli.exe validate --config configs/default-codecheck.json` 应校验：

```text
JSON 格式正确；
规则路径存在；
默认 profile 存在；
启用的工具路径存在；
默认排除配置可解析；
质量评分配置合法；
输出目录、日志目录、temp 目录可创建或可写；
```

对于 `project.root`、`input.paths` 为空的默认模板，不应报错为 fatal。

原因：

```text
default-codecheck.json 是模板，真正扫描前 GUI 会生成 temp/current-codecheck.json。
```

实际执行 `scan` 时，如果 `input.paths` 仍为空，则必须报 `ConfigError`。

### 30.6 下一步待继续设计

后续建议继续设计：

1. WPF 页面原型和 ViewModel 字段；
2. 第三方工具获取、版本固定和 release 集成脚本；
3. 样例工程和验收测试用例。

---

## 31. `report.json` 完整示例

本节定义第一版 `report.json` 的完整示例结构。

`report.json` 是所有展示和导出的核心数据源，后续以下内容都应基于它生成：

```text
HTML 报告
SARIF 报告
Excel .xlsx
GUI 扫描结果页
GUI 报告预览页
基线对比结果
误报抑制结果
质量评分
```

### 31.1 顶层结构

```json
{
  "schemaVersion": "1.0.0",
  "reportId": "20260101-103000-DemoProject",
  "tool": {},
  "project": {},
  "scan": {},
  "summary": {},
  "qualityScore": {},
  "metrics": {},
  "rules": {},
  "issues": [],
  "failedFiles": [],
  "disabledRules": [],
  "suppressedIssues": [],
  "baseline": {},
  "outputs": {},
  "logs": []
}
```

### 31.2 完整示例

```json
{
  "schemaVersion": "1.0.0",
  "reportId": "20260101-103000-DemoProject",
  "tool": {
    "name": "CodeCheck",
    "cliVersion": "1.0.0",
    "desktopVersion": "1.0.0",
    "ruleSetId": "CodeCheck-Quectel-C-Cpp",
    "ruleSetVersion": "1.0.0",
    "engines": [
      {
        "name": "clang-tidy",
        "version": "18.1.0",
        "path": "tools\\llvm\\bin\\clang-tidy.exe"
      },
      {
        "name": "cppcheck",
        "version": "2.13.0",
        "path": "tools\\cppcheck\\cppcheck.exe"
      },
      {
        "name": "lizard",
        "version": "1.17.10",
        "path": "tools\\lizard\\lizard.exe"
      },
      {
        "name": "CodeCheckBuiltin",
        "version": "1.0.0"
      }
    ]
  },
  "project": {
    "name": "DemoProject",
    "root": "D:\\项目代码\\DemoProject",
    "projectKey": "DemoProject_8F31A2",
    "description": "",
    "owner": "",
    "sourceType": "directory"
  },
  "scan": {
    "startedAt": "2026-01-01T10:30:00",
    "finishedAt": "2026-01-01T10:36:25",
    "durationSeconds": 385,
    "status": "CompletedWithFailedFiles",
    "inputType": "directory",
    "inputPaths": [
      "D:\\项目代码\\DemoProject"
    ],
    "languageStandard": {
      "c": "c11",
      "cpp": "c++14"
    },
    "headerScanPolicy": {
      "scanHeadersInDirectoryMode": false,
      "allowHeaderAsExplicitInput": true,
      "headerLanguageMode": "auto"
    },
    "totalFilesDiscovered": 230,
    "totalFilesScheduled": 128,
    "totalFilesScanned": 126,
    "totalFilesFailed": 2,
    "excludedFiles": 102,
    "maxParallelism": 4,
    "cancelled": false,
    "pausedCount": 0
  },
  "summary": {
    "totalIssues": 38,
    "activeIssues": 30,
    "suppressedIssueCount": 8,
    "newIssueCount": 12,
    "existingIssueCount": 18,
    "fixedIssueCount": 5,
    "notComparedIssueCount": 0,
    "bySeverity": {
      "Blocker": 1,
      "Critical": 4,
      "Warning": 20,
      "Suggestion": 5
    },
    "byLanguage": {
      "c": 8,
      "cpp": 22
    },
    "byEngine": {
      "clang-tidy": 12,
      "cppcheck": 10,
      "lizard": 5,
      "CodeCheckBuiltin": 3
    },
    "bySource": {
      "Huawei": 18,
      "CERT-C": 5,
      "CERT-CPP": 7
    }
  },
  "qualityScore": {
    "enabled": true,
    "baseScore": 100,
    "score": 27.5,
    "level": "高风险",
    "deduction": {
      "Blocker": {
        "count": 1,
        "pointsPerIssue": 10,
        "total": 10
      },
      "Critical": {
        "count": 4,
        "pointsPerIssue": 5,
        "total": 20
      },
      "Warning": {
        "count": 20,
        "pointsPerIssue": 2,
        "total": 40
      },
      "Suggestion": {
        "count": 5,
        "pointsPerIssue": 0.5,
        "total": 2.5
      }
    },
    "minimumScore": 0,
    "excludedSuppressedIssueCount": 8,
    "warnings": [
      "存在扫描失败文件，质量评分可能不完整。",
      "存在已关闭规则，质量评分可能偏高。",
      "存在已抑制问题，评分未包含这些问题。"
    ]
  },
  "metrics": {
    "engine": "lizard",
    "summary": {
      "functionCount": 520,
      "maxCyclomaticComplexity": 18,
      "averageCyclomaticComplexity": 4.2,
      "maxFunctionLines": 143,
      "averageFunctionLines": 23.6
    },
    "topComplexFunctions": [
      {
        "function": "ProcessMessage",
        "file": "src\\control.cpp",
        "line": 128,
        "cyclomaticComplexity": 18,
        "functionLines": 121,
        "parameterCount": 4
      },
      {
        "function": "InitSystem",
        "file": "src\\init.cpp",
        "line": 76,
        "cyclomaticComplexity": 15,
        "functionLines": 143,
        "parameterCount": 2
      }
    ],
    "topLongFunctions": [
      {
        "function": "InitSystem",
        "file": "src\\init.cpp",
        "line": 76,
        "cyclomaticComplexity": 15,
        "functionLines": 143,
        "parameterCount": 2
      },
      {
        "function": "ProcessMessage",
        "file": "src\\control.cpp",
        "line": 128,
        "cyclomaticComplexity": 18,
        "functionLines": 121,
        "parameterCount": 4
      }
    ]
  },
  "rules": {
    "totalRules": 100,
    "enabledRules": 94,
    "disabledRules": 6,
    "manualRules": 8,
    "profile": "default",
    "severityOverrides": [
      {
        "ruleId": "Quectel-CPP-020",
        "originalSeverity": "Warning",
        "currentSeverity": "Suggestion",
        "reason": "与 CERT 规则存在冲突，按 CERT 优先原则降级。"
      }
    ]
  },
  "issues": [
    {
      "issueId": "ISSUE-000001",
      "fingerprint": "sha256-stable-5f2a7d7b58c3e1a0",
      "primaryFingerprint": "sha256-primary-9a1d7b58c3e1a0ff",
      "ruleId": "Quectel-CERT-C-001",
      "ruleTitle": "字符串转换整数时必须检查转换错误",
      "sourceRuleId": "ERR34-C",
      "ruleSource": "CERT-C",
      "severity": "Critical",
      "language": "c",
      "engine": "clang-tidy",
      "engineRuleId": "cert-err34-c",
      "message": "使用 atoi 进行字符串转整数，无法可靠判断转换失败。",
      "description": "atoi 无法区分转换失败和转换结果为 0 的情况，建议使用 strtol 并检查 errno 和 endptr。",
      "suggestion": "使用 strtol/strtoul 替代 atoi，并检查转换结果。",
      "location": {
        "file": "D:\\项目代码\\DemoProject\\src\\config.c",
        "relativeFile": "src\\config.c",
        "line": 128,
        "column": 15,
        "endLine": 128,
        "endColumn": 25,
        "function": "LoadConfigValue"
      },
      "codeSnippet": {
        "startLine": 126,
        "endLine": 130,
        "text": "126: const char *input = get_value();\n127: \n128: int value = atoi(input);\n129: use_value(value);\n130:"
      },
      "baselineState": "New",
      "suppressionState": "Active",
      "isSuppressed": false,
      "isNew": true,
      "isInBaseline": false,
      "isAutoFixable": false,
      "fixSuggestion": {
        "available": true,
        "description": "改用 strtol，并检查 errno、endptr 和剩余字符。",
        "replacementExample": "char *end = NULL;\nerrno = 0;\nlong value = strtol(input, &end, 10);\nif (errno != 0 || end == input || *end != '\\0') {\n    return -1;\n}"
      },
      "references": [
        "CERT C ERR34-C"
      ],
      "tags": [
        "cert",
        "integer-conversion",
        "error-handling"
      ]
    },
    {
      "issueId": "ISSUE-000002",
      "fingerprint": "sha256-stable-6d4b8c1e3a5f0a22",
      "primaryFingerprint": "sha256-primary-0fa9b337d211aa90",
      "ruleId": "Quectel-CPP-037",
      "ruleTitle": "函数圈复杂度不得超过 10",
      "sourceRuleId": "Huawei-Complexity",
      "ruleSource": "Huawei",
      "severity": "Warning",
      "language": "cpp",
      "engine": "lizard",
      "engineRuleId": "cyclomatic-complexity",
      "message": "函数 ProcessMessage 的圈复杂度为 18，超过允许最大值 10。",
      "description": "函数分支过多会增加理解、测试和维护成本。",
      "suggestion": "拆分函数，减少 if/switch/循环嵌套，将独立逻辑提取为子函数。",
      "location": {
        "file": "D:\\项目代码\\DemoProject\\src\\control.cpp",
        "relativeFile": "src\\control.cpp",
        "line": 128,
        "column": 1,
        "endLine": 248,
        "endColumn": 1,
        "function": "ProcessMessage"
      },
      "metric": {
        "name": "cyclomaticComplexity",
        "actual": 18,
        "threshold": 10
      },
      "baselineState": "Existing",
      "suppressionState": "Active",
      "isSuppressed": false,
      "isNew": false,
      "isInBaseline": true,
      "isAutoFixable": false,
      "references": [
        "C++Languagelawer.pdf"
      ],
      "tags": [
        "huawei",
        "complexity",
        "maintainability"
      ]
    },
    {
      "issueId": "ISSUE-000003",
      "fingerprint": "sha256-stable-7acb22e4c8109a11",
      "primaryFingerprint": "sha256-primary-551b337d211bb22a",
      "ruleId": "Quectel-CPP-038",
      "ruleTitle": "函数体代码行数不得超过 100 行",
      "sourceRuleId": "Huawei-FunctionLength",
      "ruleSource": "Huawei",
      "severity": "Warning",
      "language": "cpp",
      "engine": "lizard",
      "engineRuleId": "function-length",
      "message": "函数 InitSystem 的函数体代码行数为 143 行，超过允许最大值 100。",
      "description": "函数过长会降低可读性和可测试性。",
      "suggestion": "将初始化流程拆分为多个职责单一的函数。",
      "location": {
        "file": "D:\\项目代码\\DemoProject\\src\\init.cpp",
        "relativeFile": "src\\init.cpp",
        "line": 76,
        "column": 1,
        "endLine": 219,
        "endColumn": 1,
        "function": "InitSystem"
      },
      "metric": {
        "name": "functionLines",
        "actual": 143,
        "threshold": 100
      },
      "baselineState": "New",
      "suppressionState": "Active",
      "isSuppressed": false,
      "isNew": true,
      "isInBaseline": false,
      "isAutoFixable": false,
      "references": [
        "C++Languagelawer.pdf"
      ],
      "tags": [
        "huawei",
        "function-size",
        "maintainability"
      ]
    }
  ],
  "failedFiles": [
    {
      "file": "D:\\项目代码\\DemoProject\\src\\main.cpp",
      "relativeFile": "src\\main.cpp",
      "language": "cpp",
      "engine": "clang-tidy",
      "stage": "Parse",
      "status": "Failed",
      "errorCode": "MissingInclude",
      "message": "无法找到头文件 QtCore/QObject。",
      "details": "fatal error: 'QtCore/QObject' file not found",
      "suggestion": "请补充 Qt include 路径，或从 .pro/.vcxproj 读取工程配置。",
      "command": "clang-tidy ...",
      "occurredAt": "2026-01-01T10:33:20"
    }
  ],
  "disabledRules": [
    {
      "ruleId": "Quectel-CERT-C-008",
      "title": "动态分配的内存只释放一次",
      "source": "CERT-C",
      "severity": "Blocker",
      "disabledBy": "developer",
      "disabledAt": "2026-01-01T10:00:00",
      "reason": "历史代码暂时无法整改，第一轮扫描先关闭。",
      "riskAccepted": true,
      "disableRisk": "关闭该规则后，可能漏检 double free 等严重内存问题。",
      "highlight": true
    }
  ],
  "suppressedIssues": [
    {
      "suppressionId": "SUP-000001",
      "fingerprint": "sha256-stable-2a7d7b58c3e1a0ff",
      "ruleId": "Quectel-CERT-C-002",
      "file": "src\\legacy.c",
      "line": 88,
      "reason": "确认是误报，该返回值由外层统一处理。",
      "suppressedBy": "developer",
      "suppressedAt": "2026-01-01T11:00:00",
      "expiresAt": "",
      "status": "Active",
      "scope": "single-issue"
    }
  ],
  "baseline": {
    "enabled": true,
    "mode": "compare",
    "baselinePath": "baseline\\DemoProject_8F31A2.baseline.json",
    "baselineVersion": "1.0.0",
    "existsBeforeScan": true,
    "createdAutomatically": false,
    "state": "Compared",
    "createdAt": "2025-12-20T09:00:00",
    "comparedAt": "2026-01-01T10:36:25",
    "summary": {
      "newIssues": 12,
      "existingIssues": 18,
      "fixedIssues": 5,
      "notComparedIssues": 0
    },
    "fixedIssues": [
      {
        "fingerprint": "sha256-stable-2a7d7b58c3e1a0ff",
        "ruleId": "Quectel-CPP-001",
        "file": "src\\old.cpp",
        "lastSeenAt": "2025-12-20T09:00:00",
        "message": "头文件中使用 using namespace。"
      }
    ]
  },
  "outputs": {
    "json": "reports\\DemoProject\\20260101_103000\\report.json",
    "html": "reports\\DemoProject\\20260101_103000\\report.html",
    "sarif": "reports\\DemoProject\\20260101_103000\\report.sarif",
    "xlsx": "",
    "log": "reports\\DemoProject\\20260101_103000\\scan.log"
  },
  "logs": [
    {
      "level": "Warning",
      "time": "2026-01-01T10:32:12",
      "message": "文件 src\\main.cpp 使用 clang-tidy 扫描失败。",
      "relatedFile": "src\\main.cpp"
    },
    {
      "level": "Info",
      "time": "2026-01-01T10:36:25",
      "message": "扫描完成，生成 report.json/report.html/report.sarif。"
    }
  ]
}
```

### 31.3 首次扫描自动创建基线时的差异

如果是首次扫描且 baseline 不存在，报告中的 `baseline` 节点应类似：

```json
{
  "enabled": true,
  "mode": "compare",
  "baselinePath": "baseline\\DemoProject_8F31A2.baseline.json",
  "existsBeforeScan": false,
  "createdAutomatically": true,
  "state": "Created",
  "summary": {
    "newIssues": 0,
    "existingIssues": 0,
    "fixedIssues": 0,
    "notComparedIssues": 38
  }
}
```

首次扫描时，问题的 `baselineState` 应为：

```text
NotCompared
```

原因：

```text
首次扫描没有历史基线，不能把所有问题都称为新增问题。
```

### 31.4 取消扫描时的报告差异

如果用户取消扫描，报告中的 `scan` 节点应包含：

```json
{
  "status": "Cancelled",
  "cancelled": true,
  "totalFilesDiscovered": 230,
  "totalFilesScheduled": 128,
  "totalFilesScanned": 35,
  "totalFilesFailed": 1
}
```

取消扫描时仍应尽量生成：

```text
report.json
scan.log
```

如果已有部分 issue，也应写入 `issues`。

### 31.5 配置错误时的最小失败报告

如果配置错误导致扫描未开始，应生成最小报告：

```json
{
  "schemaVersion": "1.0.0",
  "reportId": "20260101-103000-ConfigError",
  "tool": {
    "name": "CodeCheck",
    "cliVersion": "1.0.0"
  },
  "project": {
    "name": "",
    "root": ""
  },
  "scan": {
    "startedAt": "2026-01-01T10:30:00",
    "finishedAt": "2026-01-01T10:30:01",
    "durationSeconds": 1,
    "status": "ConfigError"
  },
  "summary": {
    "totalIssues": 0,
    "activeIssues": 0
  },
  "issues": [],
  "failedFiles": [],
  "logs": [
    {
      "level": "Error",
      "time": "2026-01-01T10:30:01",
      "message": "缺少 include 路径，且不允许降级扫描。"
    }
  ]
}
```

### 31.6 字段约束

关键字段约束：

| 字段 | 约束 |
|---|---|
| `schemaVersion` | 第一版固定为 `1.0.0` |
| `reportId` | 同一次扫描唯一 |
| `issueId` | 当前报告内唯一 |
| `fingerprint` | 跨扫描尽量稳定 |
| `ruleId` | 必须来自规则库或工具内置诊断规则 |
| `severity` | 只能为 `Blocker/Critical/Warning/Suggestion` |
| `baselineState` | 只能为 `New/Existing/Fixed/NotCompared` |
| `suppressionState` | 只能为 `Active/Suppressed/SuppressionExpired/SuppressionInvalid` |
| `relativeFile` | 报告展示优先使用相对路径 |
| `file` | 内部定位保留绝对路径 |

### 31.7 下一步待继续设计

后续建议继续设计：

1. 第三方工具获取、版本固定和 release 集成脚本；
2. 样例工程和验收测试用例。

---

## 32. WPF 页面原型和 ViewModel 字段

本节定义第一版 `CodeCheck.Desktop.exe` 的页面结构、页面职责、主要控件、`ViewModel` 字段和命令。

桌面端原则：

```text
WPF 只负责交互、配置、调用 CLI、展示结果；
不直接执行静态分析；
不直接解析 clang-tidy/cppcheck/lizard 原始结果；
扫描结果以 report.json 为准；
HTML 报告通过 WebView2 展示。
```

### 32.1 主窗口布局

建议采用左侧导航 + 右侧内容区域。

```text
┌───────────────────────────────────────────────┐
│ CodeCheck Desktop                              │
├───────────────┬───────────────────────────────┤
│ 首页           │                               │
│ 新建扫描       │                               │
│ 扫描配置       │          当前页面内容          │
│ 扫描进度       │                               │
│ 扫描结果       │                               │
│ 报告预览       │                               │
│ 规则管理       │                               │
│ 基线管理       │                               │
│ 误报管理       │                               │
│ 设置           │                               │
└───────────────┴───────────────────────────────┘
```

第一版导航页：

```text
HomeView
NewScanView
ScanConfigView
ScanProgressView
ResultView
ReportPreviewView
RuleConfigView
BaselineView
SuppressionView
SettingsView
```

### 32.2 `MainViewModel`

职责：管理主窗口导航、当前扫描上下文和全局状态。

建议字段：

```csharp
public sealed class MainViewModel
{
    public string ApplicationTitle { get; set; }
    public string ApplicationVersion { get; set; }
    public string RuleSetVersion { get; set; }
    public object CurrentViewModel { get; set; }
    public string CurrentPageTitle { get; set; }
    public bool IsBusy { get; set; }
    public string StatusMessage { get; set; }
    public ScanSession CurrentScanSession { get; set; }
    public ObservableCollection<NavigationItem> NavigationItems { get; set; }
}
```

建议命令：

```csharp
public ICommand NavigateCommand { get; }
public ICommand NewScanCommand { get; }
public ICommand OpenReportCommand { get; }
public ICommand OpenSettingsCommand { get; }
public ICommand ExitCommand { get; }
```

### 32.3 首页 `HomeView`

职责：快速开始扫描、打开历史报告、展示工具自检状态。

页面区域：

```text
顶部：产品名称、版本、规则库版本
快捷操作：新建扫描、打开报告、打开最近扫描
工具状态：CLI、clang-tidy、cppcheck、lizard、规则库、WebView2
最近扫描：最近 5~10 条扫描记录
```

`HomeViewModel` 字段：

```csharp
public sealed class HomeViewModel
{
    public string CliVersion { get; set; }
    public string RuleSetVersion { get; set; }
    public ObservableCollection<ToolStatusItem> ToolStatuses { get; set; }
    public ObservableCollection<RecentScanItem> RecentScans { get; set; }
    public bool HasCriticalToolError { get; set; }
    public string HealthCheckSummary { get; set; }
}
```

命令：

```csharp
public ICommand StartNewScanCommand { get; }
public ICommand OpenReportCommand { get; }
public ICommand OpenRecentScanCommand { get; }
public ICommand RefreshToolStatusCommand { get; }
public ICommand OpenReleaseDirectoryCommand { get; }
```

工具状态项：

```csharp
public sealed class ToolStatusItem
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Version { get; set; }
    public ToolStatus Status { get; set; }
    public string Message { get; set; }
}
```

状态枚举：

```csharp
public enum ToolStatus
{
    Unknown,
    Ok,
    Warning,
    Missing,
    Error
}
```

### 32.4 新建扫描页 `NewScanView`

职责：选择扫描对象。

页面区域：

```text
扫描类型：工程目录 / 单个文件 / 多个文件
路径选择：选择目录、选择文件、选择多个文件
已选择文件列表
头文件扫描提示
输出目录预览
下一步按钮
```

普通流程不显示规则集选择，默认使用 `default`。

`NewScanViewModel` 字段：

```csharp
public sealed class NewScanViewModel
{
    public ScanInputType InputType { get; set; }
    public ObservableCollection<string> SelectedPaths { get; set; }
    public ObservableCollection<SelectedFileItem> SelectedFiles { get; set; }
    public string FileListPath { get; set; }
    public string ProjectName { get; set; }
    public string ProjectRoot { get; set; }
    public string ProjectKey { get; set; }
    public string OutputDirectoryPreview { get; set; }
    public bool AllowHeaderAsExplicitInput { get; set; }
    public bool ScanHeadersInDirectoryMode { get; set; }
    public HeaderLanguageMode HeaderLanguageMode { get; set; }
    public bool CanGoNext { get; set; }
    public string ValidationMessage { get; set; }
}
```

命令：

```csharp
public ICommand SelectDirectoryCommand { get; }
public ICommand SelectSingleFileCommand { get; }
public ICommand SelectMultipleFilesCommand { get; }
public ICommand RemoveSelectedFileCommand { get; }
public ICommand ClearSelectedFilesCommand { get; }
public ICommand GoNextCommand { get; }
```

枚举：

```csharp
public enum ScanInputType
{
    Directory,
    File,
    FileList
}

public enum HeaderLanguageMode
{
    Auto,
    C,
    Cpp
}
```

### 32.5 扫描配置页 `ScanConfigView`

职责：配置编译上下文、Qt、include、宏定义、排除目录、引擎开关。

页面区域：

```text
工程文件：自动查找 .vcxproj/.pro、手动添加、移除
Include 路径：添加、删除、从工程刷新
宏定义：添加、删除、从工程刷新
Qt 配置：Qt 根目录、模块选择、自动添加 include
排除配置：默认排除目录、排除文件、排除 pattern
引擎配置：clang-tidy、cppcheck、lizard、builtin
复杂度阈值：圈复杂度 10，函数体行数 100
高级规则配置入口
开始扫描按钮
```

`ScanConfigViewModel` 字段：

```csharp
public sealed class ScanConfigViewModel
{
    public ObservableCollection<string> ProjectFiles { get; set; }
    public bool LoadFromProjectFile { get; set; }
    public bool AutoDetectProjectFiles { get; set; }
    public ObservableCollection<string> IncludeDirectories { get; set; }
    public ObservableCollection<string> Defines { get; set; }
    public string CStandard { get; set; }
    public string CppStandard { get; set; }
    public bool RequireCompileContext { get; set; }
    public bool AllowDegradedScan { get; set; }
    public QtConfigViewModel Qt { get; set; }
    public ObservableCollection<string> ExcludeDirectories { get; set; }
    public ObservableCollection<string> ExcludeFiles { get; set; }
    public ObservableCollection<string> ExcludePatterns { get; set; }
    public EngineSelectionViewModel Engines { get; set; }
    public int MaxParallelism { get; set; }
    public bool CanStartScan { get; set; }
    public string ValidationMessage { get; set; }
}
```

子模型：

```csharp
public sealed class QtConfigViewModel
{
    public bool Enabled { get; set; }
    public bool AutoDetect { get; set; }
    public string Root { get; set; }
    public ObservableCollection<QtModuleItem> Modules { get; set; }
    public ObservableCollection<string> AdditionalIncludeDirectories { get; set; }
}

public sealed class EngineSelectionViewModel
{
    public bool ClangTidyEnabled { get; set; }
    public bool CppcheckEnabled { get; set; }
    public bool LizardEnabled { get; set; }
    public bool BuiltinEnabled { get; set; }
    public int CyclomaticComplexityThreshold { get; set; }
    public int FunctionLinesThreshold { get; set; }
}
```

命令：

```csharp
public ICommand AutoDetectProjectFilesCommand { get; }
public ICommand AddProjectFileCommand { get; }
public ICommand RemoveProjectFileCommand { get; }
public ICommand AddIncludeDirectoryCommand { get; }
public ICommand RemoveIncludeDirectoryCommand { get; }
public ICommand AddDefineCommand { get; }
public ICommand RemoveDefineCommand { get; }
public ICommand SelectQtRootCommand { get; }
public ICommand OpenAdvancedRuleConfigCommand { get; }
public ICommand ValidateConfigCommand { get; }
public ICommand StartScanCommand { get; }
```

### 32.6 扫描进度页 `ScanProgressView`

职责：显示 CLI 实时进度，支持暂停、继续、取消。

页面区域：

```text
扫描状态
当前阶段
当前引擎
当前文件
总文件数、已扫描数、失败数
问题数量统计
耗时
进度条
实时日志
暂停 / 继续 / 取消
```

`ScanProgressViewModel` 字段：

```csharp
public sealed class ScanProgressViewModel
{
    public string ScanStatus { get; set; }
    public string CurrentStage { get; set; }
    public string CurrentEngine { get; set; }
    public string CurrentFile { get; set; }
    public int TotalFiles { get; set; }
    public int ScannedFiles { get; set; }
    public int FailedFiles { get; set; }
    public int TotalIssues { get; set; }
    public int BlockerCount { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int SuggestionCount { get; set; }
    public double ProgressPercent { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public bool CanPause { get; set; }
    public bool CanResume { get; set; }
    public bool CanCancel { get; set; }
    public ObservableCollection<ScanLogItem> Logs { get; set; }
}
```

命令：

```csharp
public ICommand PauseCommand { get; }
public ICommand ResumeCommand { get; }
public ICommand CancelCommand { get; }
public ICommand OpenLogCommand { get; }
public ICommand OpenReportAfterCompletedCommand { get; }
```

### 32.7 扫描结果页 `ResultView`

职责：展示 `report.json` 中的问题列表、摘要、评分、筛选和误报操作。

页面区域：

```text
顶部摘要：质量评分、总问题、新增、历史、已修复、已抑制、失败文件
级别统计：阻断、严重、警告、建议
筛选栏：级别、基线状态、规则、文件、引擎、是否抑制
问题列表
问题详情
代码片段
修复建议
操作：标记误报、复制问题、打开文件、打开报告
```

`ResultViewModel` 字段：

```csharp
public sealed class ResultViewModel
{
    public string ReportPath { get; set; }
    public ScanReport Report { get; set; }
    public QualityScoreViewModel QualityScore { get; set; }
    public ResultSummaryViewModel Summary { get; set; }
    public ObservableCollection<IssueItemViewModel> Issues { get; set; }
    public ICollectionView FilteredIssues { get; set; }
    public IssueItemViewModel SelectedIssue { get; set; }
    public IssueFilterViewModel Filter { get; set; }
    public ObservableCollection<FailedFileItemViewModel> FailedFiles { get; set; }
    public ObservableCollection<DisabledRuleItemViewModel> DisabledRules { get; set; }
}
```

筛选模型：

```csharp
public sealed class IssueFilterViewModel
{
    public string Keyword { get; set; }
    public RuleSeverity? Severity { get; set; }
    public BaselineState? BaselineState { get; set; }
    public string Engine { get; set; }
    public string RuleId { get; set; }
    public string FilePath { get; set; }
    public bool OnlyNewIssues { get; set; }
    public bool OnlyActiveIssues { get; set; }
    public bool OnlyBlockerAndCritical { get; set; }
}
```

命令：

```csharp
public ICommand LoadReportCommand { get; }
public ICommand RefreshFilterCommand { get; }
public ICommand ClearFilterCommand { get; }
public ICommand MarkAsSuppressedCommand { get; }
public ICommand CopyIssueCommand { get; }
public ICommand OpenFileCommand { get; }
public ICommand OpenReportPreviewCommand { get; }
public ICommand ExportXlsxCommand { get; }
```

### 32.8 报告预览页 `ReportPreviewView`

职责：使用 `WebView2` 展示 `report.html`。

页面区域：

```text
工具栏：打开 HTML、打开目录、导出 Excel、刷新
WebView2 报告区域
```

`ReportPreviewViewModel` 字段：

```csharp
public sealed class ReportPreviewViewModel
{
    public string HtmlReportPath { get; set; }
    public string JsonReportPath { get; set; }
    public string SarifReportPath { get; set; }
    public string XlsxReportPath { get; set; }
    public bool HtmlReportExists { get; set; }
    public bool CanExportXlsx { get; set; }
    public string StatusMessage { get; set; }
}
```

命令：

```csharp
public ICommand LoadHtmlReportCommand { get; }
public ICommand RefreshCommand { get; }
public ICommand OpenHtmlExternallyCommand { get; }
public ICommand OpenReportDirectoryCommand { get; }
public ICommand ExportXlsxCommand { get; }
public ICommand OpenJsonCommand { get; }
public ICommand OpenSarifCommand { get; }
```

### 32.9 规则管理页 `RuleConfigView`

职责：查看规则、筛选规则、关闭规则、填写关闭原因、提示风险。

普通流程不进入该页面，只有特殊要求才修改规则。

页面区域：

```text
规则统计：总数、启用、关闭、manual
筛选栏：来源、语言、级别、引擎、启用状态
规则列表
规则详情：说明、错误示例、正确示例、修复建议、关闭风险
关闭规则弹窗：原因 + 风险确认
```

`RuleConfigViewModel` 字段：

```csharp
public sealed class RuleConfigViewModel
{
    public string RuleSetVersion { get; set; }
    public string CurrentProfile { get; set; }
    public ObservableCollection<RuleItemViewModel> Rules { get; set; }
    public ICollectionView FilteredRules { get; set; }
    public RuleItemViewModel SelectedRule { get; set; }
    public RuleFilterViewModel Filter { get; set; }
    public int TotalRuleCount { get; set; }
    public int EnabledRuleCount { get; set; }
    public int DisabledRuleCount { get; set; }
    public int ManualRuleCount { get; set; }
}
```

命令：

```csharp
public ICommand LoadRulesCommand { get; }
public ICommand EnableRuleCommand { get; }
public ICommand DisableRuleCommand { get; }
public ICommand ClearRuleFilterCommand { get; }
public ICommand SaveRuleConfigCommand { get; }
public ICommand ResetToDefaultProfileCommand { get; }
```

关闭规则弹窗字段：

```csharp
public sealed class DisableRuleDialogViewModel
{
    public string RuleId { get; set; }
    public string RuleTitle { get; set; }
    public RuleSeverity Severity { get; set; }
    public string DisableRisk { get; set; }
    public string Reason { get; set; }
    public bool RiskAccepted { get; set; }
    public bool IsHighRiskRule { get; set; }
    public bool CanConfirm { get; set; }
}
```

### 32.10 基线管理页 `BaselineView`

职责：查看基线文件、基线状态、更新或重新生成基线。

页面区域：

```text
当前基线路径
是否存在
创建时间 / 更新时间
基线问题数量
本次对比：新增、历史、已修复
操作：更新基线、重新生成基线、打开基线文件
```

`BaselineViewModel` 字段：

```csharp
public sealed class BaselineViewModel
{
    public string BaselinePath { get; set; }
    public bool BaselineExists { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int TotalBaselineIssues { get; set; }
    public int NewIssues { get; set; }
    public int ExistingIssues { get; set; }
    public int FixedIssues { get; set; }
    public string StatusMessage { get; set; }
}
```

命令：

```csharp
public ICommand LoadBaselineCommand { get; }
public ICommand UpdateBaselineCommand { get; }
public ICommand RecreateBaselineCommand { get; }
public ICommand OpenBaselineFileCommand { get; }
public ICommand OpenBaselineDirectoryCommand { get; }
```

更新基线确认提示：

```text
更新基线后，当前未抑制问题将被视为历史问题。
后续扫描将主要突出显示新增问题。是否继续？
```

### 32.11 误报管理页 `SuppressionView`

职责：查看和管理 `suppression.json`。

页面区域：

```text
误报文件路径
误报数量
误报列表
筛选：规则、文件、状态、范围
操作：取消抑制、打开 suppression.json、导出误报列表
```

`SuppressionViewModel` 字段：

```csharp
public sealed class SuppressionViewModel
{
    public string SuppressionPath { get; set; }
    public bool SuppressionFileExists { get; set; }
    public ObservableCollection<SuppressionItemViewModel> Suppressions { get; set; }
    public ICollectionView FilteredSuppressions { get; set; }
    public SuppressionItemViewModel SelectedSuppression { get; set; }
    public int ActiveSuppressionCount { get; set; }
    public int DisabledSuppressionCount { get; set; }
    public string StatusMessage { get; set; }
}
```

命令：

```csharp
public ICommand LoadSuppressionsCommand { get; }
public ICommand DisableSuppressionCommand { get; }
public ICommand OpenSuppressionFileCommand { get; }
public ICommand OpenSuppressionDirectoryCommand { get; }
public ICommand ExportSuppressionListCommand { get; }
```

### 32.12 设置页 `SettingsView`

职责：配置默认路径、工具状态、并发数、日志级别。

页面区域：

```text
release 根目录
工具路径：CLI、clang-tidy、cppcheck、lizard
规则路径
默认报告目录
默认 baseline 目录
默认 suppression 目录
默认日志目录
默认并发数
日志级别
工具状态检测
```

`SettingsViewModel` 字段：

```csharp
public sealed class SettingsViewModel
{
    public string ReleaseRoot { get; set; }
    public string CliPath { get; set; }
    public string ClangTidyPath { get; set; }
    public string CppcheckPath { get; set; }
    public string LizardPath { get; set; }
    public string RuleIndexPath { get; set; }
    public string DefaultConfigPath { get; set; }
    public string ReportsDirectory { get; set; }
    public string BaselineDirectory { get; set; }
    public string SuppressionsDirectory { get; set; }
    public string LogsDirectory { get; set; }
    public string TempDirectory { get; set; }
    public int MaxParallelism { get; set; }
    public string LogLevel { get; set; }
    public ObservableCollection<ToolStatusItem> ToolStatuses { get; set; }
}
```

命令：

```csharp
public ICommand SaveSettingsCommand { get; }
public ICommand ResetSettingsCommand { get; }
public ICommand CheckToolsCommand { get; }
public ICommand OpenReleaseDirectoryCommand { get; }
public ICommand OpenLogsDirectoryCommand { get; }
```

### 32.13 标记误报弹窗

触发入口：`ResultView` 中选择问题后点击“标记为误报”。

弹窗字段：

```csharp
public sealed class MarkSuppressionDialogViewModel
{
    public string IssueId { get; set; }
    public string RuleId { get; set; }
    public string RuleTitle { get; set; }
    public string File { get; set; }
    public int Line { get; set; }
    public SuppressionScope Scope { get; set; }
    public string Reason { get; set; }
    public bool CanConfirm { get; set; }
}
```

范围枚举：

```csharp
public enum SuppressionScope
{
    SingleIssue,
    FileRule,
    PathRule
}
```

默认范围：

```text
SingleIssue
```

原因必须填写。

### 32.14 `CliRunnerService`

职责：启动 CLI、读取 JSON Lines、写控制文件、返回扫描结果。

接口建议：

```csharp
public interface ICliRunnerService
{
    Task<CliRunResult> RunScanAsync(
        string configPath,
        IProgress<CliProgressEvent> progress,
        CancellationToken cancellationToken);

    Task<CliRunResult> RunValidateAsync(
        string configPath,
        CancellationToken cancellationToken);

    Task PauseAsync(string controlFilePath);
    Task ResumeAsync(string controlFilePath);
    Task CancelAsync(string controlFilePath);
}
```

`CliProgressEvent`：

```csharp
public sealed class CliProgressEvent
{
    public string Type { get; set; }
    public string Stage { get; set; }
    public string Engine { get; set; }
    public string File { get; set; }
    public int Current { get; set; }
    public int Total { get; set; }
    public string Severity { get; set; }
    public string RuleId { get; set; }
    public string Message { get; set; }
    public string ReportPath { get; set; }
}
```

### 32.15 GUI 生成配置流程

GUI 开始扫描前执行：

```text
1. 读取 configs/default-codecheck.json
2. 根据 NewScanView 覆盖 project/input
3. 根据 ScanConfigView 覆盖 build/scan/engines/runtime
4. 如果未进入高级规则配置，保持 rules.profile=default
5. 根据 projectKey 自动生成 baseline.path
6. 根据 projectKey 自动生成 suppression.path
7. 生成 report.outputDirectory
8. 写入 temp/current-codecheck.json
9. 调用 CodeCheck.Cli.exe scan --config temp/current-codecheck.json
```

### 32.16 GUI 第一版实现优先级

建议实现顺序：

```text
P0：MainWindow + 导航框架
P1：首页工具状态检测
P2：新建扫描页
P3：扫描配置页
P4：CLI 调用与扫描进度页
P5：加载 report.json 的扫描结果页
P6：WebView2 报告预览页
P7：标记误报弹窗
P8：基线管理页
P9：规则管理页
P10：设置页
P11：Excel 导出入口
```

### 32.17 下一步待继续设计

后续建议继续设计：

1. 样例工程和验收测试用例。

---

## 33. 第三方工具获取、版本固定和 release 集成脚本

本节定义第一版第三方工具的获取方式、版本固定策略、目录放置方式、许可证文件要求和 `release` 集成脚本设计。

第三方工具包括：

```text
LLVM/Clang-Tidy
Cppcheck
Lizard
WebView2 Runtime
```

### 33.1 总体原则

第三方工具集成遵循以下原则：

```text
固定版本；
统一放在 release/tools 目录下；
不要求用户单独安装 clang-tidy/cppcheck/lizard；
优先作为外部命令行工具调用，不链接源码或库；
不修改第三方源码；
保留第三方许可证和来源说明；
启动时做完整性自检；
扫描报告中记录第三方工具版本。
```

### 33.2 版本固定建议

第一版建议固定版本如下，实际版本可在落地时根据可用性调整：

| 工具 | 建议版本 | 用途 | 目录 |
|---|---|---|---|
| `LLVM/Clang-Tidy` | `18.x` 或团队验证稳定版本 | C/C++ AST 静态分析 | `tools/llvm` |
| `Cppcheck` | `2.13.x` 或团队验证稳定版本 | C/C++ 缺陷检查 | `tools/cppcheck` |
| `Lizard` | `1.17.x` 或团队验证稳定版本 | 圈复杂度、函数长度 | `tools/lizard` |
| `WebView2 Runtime` | 系统已安装或 Fixed Version | HTML 报告预览 | 可选 `tools/webview2` |

版本固定信息记录在：

```text
release/tools/third-party-versions.json
release/licenses/THIRD-PARTY-NOTICES.txt
```

### 33.3 `third-party-versions.json`

建议文件：

```text
tools/third-party-versions.json
```

示例：

```json
{
  "llvm": {
    "name": "LLVM/Clang-Tidy",
    "version": "18.1.0",
    "path": "tools\\llvm\\bin\\clang-tidy.exe",
    "license": "Apache License 2.0 with LLVM Exceptions",
    "source": "https://github.com/llvm/llvm-project",
    "modified": false
  },
  "cppcheck": {
    "name": "Cppcheck",
    "version": "2.13.0",
    "path": "tools\\cppcheck\\cppcheck.exe",
    "license": "GPL",
    "source": "https://github.com/danmar/cppcheck",
    "modified": false
  },
  "lizard": {
    "name": "Lizard",
    "version": "1.17.10",
    "path": "tools\\lizard\\lizard.exe",
    "license": "MIT",
    "source": "https://github.com/terryyin/lizard",
    "modified": false
  },
  "webview2": {
    "name": "Microsoft Edge WebView2 Runtime",
    "version": "system",
    "path": "system",
    "license": "Microsoft Software License Terms",
    "source": "https://developer.microsoft.com/microsoft-edge/webview2/",
    "modified": false
  }
}
```

说明：

```text
这里的 license 字段需要在正式发布前根据实际下载包再次核对。
```

### 33.4 `tools` 目录结构

`release/tools` 建议结构：

```text
tools
├── third-party-versions.json
├── llvm
│   ├── bin
│   │   ├── clang-tidy.exe
│   │   ├── clang.exe
│   │   ├── clang++.exe
│   │   └── *.dll
│   └── ...
│
├── cppcheck
│   ├── cppcheck.exe
│   ├── cfg
│   ├── platforms
│   └── ...
│
├── lizard
│   ├── lizard.exe
│   └── ...
│
└── webview2
    └── 可选 Fixed Version Runtime
```

### 33.5 LLVM/Clang-Tidy 集成策略

#### 33.5.1 获取方式

可选方式：

```text
方式 A：使用 LLVM 官方 Windows release 包；
方式 B：使用团队验证过的 LLVM 压缩包；
方式 C：从已有安装目录提取必要文件。
```

第一版建议：

```text
使用团队验证过的 LLVM Windows 压缩包，解压到 tools/llvm。
```

#### 33.5.2 必需文件

至少需要验证包含：

```text
tools/llvm/bin/clang-tidy.exe
tools/llvm/bin/clang.exe
tools/llvm/bin/clang++.exe
tools/llvm/bin/*.dll
```

实际依赖以运行自检为准。

#### 33.5.3 自检命令

```powershell
tools\llvm\bin\clang-tidy.exe --version
```

期望：

```text
能正常输出版本号，退出码为 0。
```

#### 33.5.4 调用策略

按文件调用：

```text
clang-tidy.exe file.cpp --checks=... -- -std=c++14 -Ixxx -Dxxx
```

C 文件：

```text
clang-tidy.exe file.c --checks=... -- -std=c11 -Ixxx -Dxxx
```

头文件显式扫描：

```text
clang-tidy.exe file.hpp --checks=... -- -x c++ -std=c++14 -Ixxx -Dxxx
```

### 33.6 Cppcheck 集成策略

#### 33.6.1 获取方式

可选方式：

```text
方式 A：使用 Cppcheck 官方 Windows 版本；
方式 B：团队内部固定版本压缩包；
方式 C：从已有安装目录复制必要目录。
```

第一版建议：

```text
使用固定版本 Cppcheck，完整保留 cppcheck.exe、cfg、platforms 等目录。
```

#### 33.6.2 必需文件

```text
tools/cppcheck/cppcheck.exe
tools/cppcheck/cfg
tools/cppcheck/platforms
```

`cfg` 目录不能省略，否则会影响库函数和平台识别。

#### 33.6.3 自检命令

```powershell
tools\cppcheck\cppcheck.exe --version
```

#### 33.6.4 调用策略

默认参数：

```text
--enable=warning,style,performance,portability
--xml
--xml-version=2
```

第一版不启用：

```text
--inconclusive
```

原因：

```text
降低误报。
```

### 33.7 Lizard 集成策略

#### 33.7.1 获取方式

`Lizard` 仓库：

```text
https://github.com/terryyin/lizard
```

可选方式：

```text
方式 A：随 release 内置 Python + lizard；
方式 B：使用 PyInstaller 打包成 lizard.exe；
方式 C：要求用户本机安装 Python 和 lizard。
```

第一版建议：

```text
使用 PyInstaller 将 lizard 打包成 tools/lizard/lizard.exe。
```

原因：

```text
用户无需安装 Python；
release 复制即用；
CLI 调用简单；
版本可固定。
```

#### 33.7.2 自检命令

```powershell
tools\lizard\lizard.exe --version
```

如果打包后的 `lizard.exe` 不支持 `--version`，则使用：

```powershell
tools\lizard\lizard.exe --help
```

#### 33.7.3 调用策略

建议按批次调用：

```text
lizard.exe file1.cpp file2.cpp file3.c
```

如果需要区分语言：

```text
lizard.exe -l cpp file.cpp
lizard.exe -l c file.c
```

第一版关注字段：

```text
函数名
文件
行号
NLOC / length
CCN
参数数量
```

### 33.8 WebView2 集成策略

WPF 使用 `WebView2` 展示本地 HTML 报告。

可选策略：

```text
方案 A：使用系统已安装 WebView2 Runtime；
方案 B：随 release 携带 Fixed Version Runtime。
```

第一版建议：

```text
先检测系统 WebView2 Runtime。
如果缺失，在 GUI 首页显示错误提示，并在 docs 中说明安装方式。
```

后续如果需要完全离线复制即用，可增加：

```text
tools/webview2/FixedVersionRuntime
```

### 33.9 许可证目录

`release/licenses` 必须包含：

```text
LLVM-LICENSE.txt
Cppcheck-LICENSE.txt
Lizard-LICENSE.txt
WebView2-LICENSE.txt
THIRD-PARTY-NOTICES.txt
```

`THIRD-PARTY-NOTICES.txt` 建议格式：

```text
CodeCheck Desktop Third-Party Notices

1. LLVM/Clang-Tidy
   Version: 18.1.0
   Source: https://github.com/llvm/llvm-project
   License: Apache License 2.0 with LLVM Exceptions
   Usage: External command-line static analysis engine
   Modified: No

2. Cppcheck
   Version: 2.13.0
   Source: https://github.com/danmar/cppcheck
   License: GPL
   Usage: External command-line static analysis engine
   Modified: No

3. Lizard
   Version: 1.17.10
   Source: https://github.com/terryyin/lizard
   License: MIT
   Usage: External command-line complexity metrics tool
   Modified: No

4. Microsoft Edge WebView2 Runtime
   Version: System Runtime or Fixed Version Runtime
   Source: https://developer.microsoft.com/microsoft-edge/webview2/
   License: Microsoft Software License Terms
   Usage: Local HTML report preview in WPF
   Modified: No
```

说明：

```text
正式发布前必须核对各组件实际版本和许可证文本。
```

### 33.10 release 集成脚本设计

建议提供 PowerShell 脚本：

```text
scripts/build-release.ps1
scripts/check-release.ps1
scripts/clean-release.ps1
```

### 33.11 `build-release.ps1`

职责：生成完整 `release` 目录。

主要步骤：

```text
1. 清理旧 release；
2. 创建 release 目录结构；
3. dotnet publish CodeCheck.Cli；
4. dotnet publish CodeCheck.Desktop；
5. 复制 CodeCheck.Core/Reporting 相关 dll；
6. 复制 rules；
7. 复制 configs；
8. 复制 report-templates；
9. 复制 docs；
10. 复制 samples；
11. 复制 tools/llvm；
12. 复制 tools/cppcheck；
13. 复制 tools/lizard；
14. 复制 licenses；
15. 创建 reports/baseline/suppressions/logs/temp 空目录；
16. 生成或复制 tools/third-party-versions.json；
17. 执行 check-release.ps1。
```

脚本参数建议：

```powershell
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "release",
    [switch]$SelfContained,
    [switch]$SkipTools
)
```

发布方式建议：

```text
第一版可以先使用 framework-dependent 发布；
若希望减少 .NET Runtime 依赖，可改为 self-contained 发布。
```

### 33.12 `check-release.ps1`

职责：检查 `release` 是否完整。

检查项：

```text
CodeCheck.Desktop.exe 是否存在；
CodeCheck.Cli.exe 是否存在；
tools/llvm/bin/clang-tidy.exe 是否存在；
tools/cppcheck/cppcheck.exe 是否存在；
tools/lizard/lizard.exe 是否存在；
rules/rules.index.json 是否存在；
configs/default-codecheck.json 是否存在；
report-templates 是否存在；
licenses 是否存在；
reports/baseline/suppressions/logs/temp 是否存在；
能否执行 clang-tidy --version；
能否执行 cppcheck --version；
能否执行 lizard --help 或 --version；
CodeCheck.Cli.exe validate 是否可运行。
```

输出示例：

```text
[OK] CodeCheck.Cli.exe
[OK] CodeCheck.Desktop.exe
[OK] clang-tidy 18.1.0
[OK] cppcheck 2.13.0
[OK] lizard 1.17.10
[OK] rules.index.json
[OK] default-codecheck.json
[ERROR] WebView2 Runtime 未检测到
```

### 33.13 `clean-release.ps1`

职责：清理生成目录。

默认清理：

```text
release 下的程序文件、rules、configs、templates、tools、licenses、docs、samples
```

默认不清理用户数据：

```text
reports
baseline
suppressions
logs
```

如需完全清理，提供参数：

```powershell
-Full
```

### 33.14 第三方工具目录来源约定

建议在开发仓库中准备本地目录：

```text
third_party_tools
├── llvm
├── cppcheck
└── lizard
```

`build-release.ps1` 从该目录复制到：

```text
release/tools
```

注意：

```text
third_party_tools 可根据公司合规策略决定是否纳入版本库；
如果工具包较大，可不提交，只在构建机或开发机本地准备。
```

### 33.15 release 自检和 GUI 自检关系

`check-release.ps1` 用于打包阶段检查。

`CodeCheck.Desktop.exe` 首页自检用于运行阶段检查。

两者检查项应保持一致：

```text
CLI
clang-tidy
cppcheck
lizard
rules.index.json
default-codecheck.json
WebView2 Runtime
目录写权限
```

### 33.16 不建议第一版做的集成方式

第一版不建议：

```text
通过 NuGet 或 pip 在用户机器上动态安装 lizard；
运行时自动下载 LLVM；
运行时自动下载 Cppcheck；
依赖用户 PATH 中的 clang-tidy/cppcheck/lizard；
修改第三方工具源码；
链接 Cppcheck 或 LLVM 作为库。
```

原因：

```text
环境不可控；
扫描结果不稳定；
许可证和合规风险更高；
不符合 release 复制即用目标。
```

### 33.17 下一步待继续设计

后续建议继续设计：

1. 第一版实施前最终检查清单；
2. 开始创建工程骨架和基础文件。

---

## 34. 样例工程和验收测试用例

本节定义第一版需要准备的样例工程、测试数据、验收场景和通过标准。

样例工程用于：

```text
验证 CLI 扫描流程；
验证第三方工具集成；
验证 report.json/report.html/report.sarif/report.xlsx；
验证 GUI 操作流程；
验证 baseline 和 suppression；
验证中文路径、空格路径、头文件显式扫描；
作为后续回归测试数据。
```

### 34.1 样例工程目录结构

建议在仓库中准备：

```text
samples
├── c-demo
│   ├── include
│   │   └── c_demo.h
│   ├── src
│   │   ├── main.c
│   │   ├── unsafe_string.c
│   │   ├── memory_error.c
│   │   ├── complexity.c
│   │   └── long_function.c
│   └── README.md
│
├── cpp-demo
│   ├── include
│   │   ├── demo.hpp
│   │   └── bad_header.hpp
│   ├── src
│   │   ├── main.cpp
│   │   ├── class_error.cpp
│   │   ├── memory_error.cpp
│   │   ├── exception_error.cpp
│   │   ├── complexity.cpp
│   │   └── long_function.cpp
│   └── README.md
│
└── qt-demo
    ├── qt-demo.pro
    ├── include
    │   └── mainwindow.h
    ├── src
    │   ├── main.cpp
    │   ├── mainwindow.cpp
    │   ├── qt_signal_slot.cpp
    │   └── complexity.cpp
    ├── ui
    │   └── mainwindow.ui
    └── README.md
```

### 34.2 `c-demo` 样例目标

`c-demo` 用于验证 C 语言规则、C11 标准、危险函数、内存错误、复杂度和函数长度。

建议包含问题：

| 文件 | 问题类型 | 预期规则 |
|---|---|---|
| `unsafe_string.c` | 使用 `gets` | `Quectel-C-001` |
| `unsafe_string.c` | 使用不安全字符串拷贝 | `Quectel-C-002` / `Quectel-CERT-C-006` |
| `unsafe_string.c` | 使用 `sprintf` | `Quectel-C-003` |
| `memory_error.c` | 动态内存申请未检查 | `Quectel-C-007` / `Quectel-CERT-C-009` |
| `memory_error.c` | 内存泄漏 | `Quectel-C-008` |
| `memory_error.c` | double free | `Quectel-C-009` / `Quectel-CERT-C-008` |
| `memory_error.c` | use after free | `Quectel-C-010` / `Quectel-CERT-C-007` |
| `complexity.c` | 圈复杂度超过 10 | `Quectel-C-021` |
| `long_function.c` | 函数体超过 100 行 | `Quectel-C-022` |

### 34.3 `cpp-demo` 样例目标

`cpp-demo` 用于验证 C++14 规则、类规则、RAII、异常、头文件规则、复杂度和函数长度。

建议包含问题：

| 文件 | 问题类型 | 预期规则 |
|---|---|---|
| `bad_header.hpp` | 头文件使用 `using namespace` | `Quectel-CPP-001` |
| `bad_header.hpp` | 头文件定义非 `inline` 函数 | `Quectel-CPP-002` |
| `class_error.cpp` | 有虚函数但析构函数非 virtual | `Quectel-CPP-005` / `Quectel-CERT-CPP-001` |
| `class_error.cpp` | 构造/析构函数调用虚函数 | `Quectel-CPP-008` |
| `class_error.cpp` | 对象切片 | `Quectel-CPP-036` / `Quectel-CERT-CPP-014` |
| `memory_error.cpp` | 裸 `new/delete` 管理资源 | `Quectel-CPP-010` |
| `memory_error.cpp` | use after free | `Quectel-CPP-012` / `Quectel-CERT-CPP-005` |
| `exception_error.cpp` | 析构函数抛出异常 | `Quectel-CPP-025` / `Quectel-CERT-CPP-003` |
| `exception_error.cpp` | catch 后完全忽略 | `Quectel-CPP-024` / `Quectel-CERT-CPP-011` |
| `complexity.cpp` | 圈复杂度超过 10 | `Quectel-CPP-037` |
| `long_function.cpp` | 函数体超过 100 行 | `Quectel-CPP-038` |

### 34.4 `qt-demo` 样例目标

`qt-demo` 用于验证 Qt include 配置、`.pro` 读取、Qt 生成文件排除和 C++14 扫描。

建议包含内容：

```text
qt-demo.pro 中配置 INCLUDEPATH、DEFINES、QT += core gui widgets；
src/mainwindow.cpp 使用 QObject/QWidget 等 Qt 类型；
ui/mainwindow.ui 用于模拟 Qt 工程结构；
生成文件如 moc_*.cpp、ui_*.h、qrc_*.cpp 应被默认排除；
complexity.cpp 中构造复杂函数验证 lizard。
```

预期验证点：

| 场景 | 预期 |
|---|---|
| 未配置 Qt include | 扫描前配置校验失败或 clang-tidy 失败文件可见 |
| 从 `.pro` 读取 Qt 配置 | 能补充 include 和宏定义 |
| 存在 `moc_*.cpp` | 默认排除 |
| 存在 `ui_*.h` | 默认排除 |
| 显式选择 `mainwindow.h` | 允许扫描头文件 |

### 34.5 目录扫描测试用例

| 编号 | 用例 | 输入 | 预期 |
|---|---|---|---|
| `TC-DIR-001` | 扫描 C 工程目录 | `samples/c-demo` | 生成 `report.json/html/sarif` |
| `TC-DIR-002` | 扫描 C++ 工程目录 | `samples/cpp-demo` | 生成 `report.json/html/sarif` |
| `TC-DIR-003` | 扫描 Qt 工程目录 | `samples/qt-demo` | 能读取 `.pro` 或要求用户配置 Qt 路径 |
| `TC-DIR-004` | 目录扫描默认不把头文件作为入口 | `samples/cpp-demo` | 头文件不作为独立入口，但可被源文件引用 |
| `TC-DIR-005` | 默认排除第三方目录 | 添加 `third_party/bad.cpp` | 不扫描该文件 |
| `TC-DIR-006` | 默认排除构建目录 | 添加 `build/bad.cpp` | 不扫描该文件 |

### 34.6 单文件和多文件测试用例

| 编号 | 用例 | 输入 | 预期 |
|---|---|---|---|
| `TC-FILE-001` | 扫描单个 `.c` 文件 | `samples/c-demo/src/unsafe_string.c` | 按 C 规则扫描 |
| `TC-FILE-002` | 扫描单个 `.cpp` 文件 | `samples/cpp-demo/src/class_error.cpp` | 按 C++14 规则扫描 |
| `TC-FILE-003` | 显式扫描 `.h` 文件 | `samples/c-demo/include/c_demo.h` | 允许扫描，语言模式自动或手动指定 |
| `TC-FILE-004` | 显式扫描 `.hpp` 文件 | `samples/cpp-demo/include/bad_header.hpp` | 检出头文件规则 |
| `TC-FILE-005` | 扫描多文件列表 | `selected-files.txt` | 只扫描清单文件 |
| `TC-FILE-006` | 多文件中包含不存在文件 | `selected-files.txt` | 报告 failedFiles 或配置错误，行为需一致 |

### 34.7 编译上下文测试用例

| 编号 | 用例 | 预期 |
|---|---|---|
| `TC-BUILD-001` | include 路径为空且 `allowDegradedScan=false` | 阻断扫描，`ConfigError` |
| `TC-BUILD-002` | 手动添加 include 路径 | 可扫描 |
| `TC-BUILD-003` | 从 `.vcxproj` 读取 include 和宏 | 可扫描 |
| `TC-BUILD-004` | 从 `.pro` 读取 include 和宏 | 可扫描 |
| `TC-BUILD-005` | Qt 路径错误 | 配置错误或 failedFiles 中明确提示 |
| `TC-BUILD-006` | `.h` 文件语言模式指定 C | 使用 C 规则 |
| `TC-BUILD-007` | `.h` 文件语言模式指定 C++ | 使用 C++14 规则 |

### 34.8 第三方引擎测试用例

| 编号 | 用例 | 预期 |
|---|---|---|
| `TC-ENGINE-001` | `clang-tidy` 缺失 | `validate` 报 `DependencyError` |
| `TC-ENGINE-002` | `cppcheck` 缺失 | `validate` 报 `DependencyError` |
| `TC-ENGINE-003` | `lizard` 缺失 | `validate` 报 `DependencyError` |
| `TC-ENGINE-004` | `lizard` 复杂度检测 | 生成复杂度 issue 和 metrics |
| `TC-ENGINE-005` | `lizard` 函数长度检测 | 生成函数长度 issue 和 metrics |
| `TC-ENGINE-006` | `cppcheck` 检测内存问题 | 生成 cppcheck issue |
| `TC-ENGINE-007` | `clang-tidy` 检测 C++ 类问题 | 生成 clang-tidy issue |
| `TC-ENGINE-008` | 单文件 clang-tidy 失败 | 不影响 cppcheck/lizard 继续扫描 |

### 34.9 报告生成测试用例

| 编号 | 用例 | 预期 |
|---|---|---|
| `TC-REPORT-001` | 生成 `report.json` | 字段完整，JSON 可解析 |
| `TC-REPORT-002` | 生成 `report.html` | 浏览器可打开，中文正常 |
| `TC-REPORT-003` | 生成 `report.sarif` | SARIF 基本结构有效 |
| `TC-REPORT-004` | 导出 `report.xlsx` | Excel 可打开 |
| `TC-REPORT-005` | 中文路径扫描 | 报告路径和文件名显示正常 |
| `TC-REPORT-006` | 空格路径扫描 | 报告路径和文件名显示正常 |
| `TC-REPORT-007` | 存在 failedFiles | HTML 和 JSON 均显示失败文件 |
| `TC-REPORT-008` | 存在 disabledRules | HTML 和 JSON 均显示关闭规则 |

### 34.10 基线测试用例

| 编号 | 用例 | 预期 |
|---|---|---|
| `TC-BASELINE-001` | 首次扫描 baseline 不存在 | 自动创建 baseline |
| `TC-BASELINE-002` | 首次扫描问题状态 | `NotCompared` |
| `TC-BASELINE-003` | 第二次相同代码扫描 | 问题标记为 `Existing` |
| `TC-BASELINE-004` | 新增一处问题后扫描 | 新问题标记为 `New` |
| `TC-BASELINE-005` | 修复一处历史问题后扫描 | 历史问题进入 `Fixed` |
| `TC-BASELINE-006` | 更新基线 | 当前未抑制问题写入 baseline |

### 34.11 误报抑制测试用例

| 编号 | 用例 | 预期 |
|---|---|---|
| `TC-SUP-001` | 标记单个问题为误报 | 写入 suppression，问题不参与评分 |
| `TC-SUP-002` | 按文件 + 规则抑制 | 同文件同规则问题被抑制 |
| `TC-SUP-003` | 按目录 + 规则抑制 | 同目录同规则问题被抑制 |
| `TC-SUP-004` | 取消抑制 | suppression 状态变为 `Disabled` |
| `TC-SUP-005` | 抑制原因为空 | GUI 不允许确认 |
| `TC-SUP-006` | 已抑制问题展示 | 报告中单独展示 suppressedIssues |

### 34.12 GUI 验收测试用例

| 编号 | 用例 | 预期 |
|---|---|---|
| `TC-GUI-001` | 启动桌面端 | 首页显示，工具状态自检完成 |
| `TC-GUI-002` | 新建目录扫描 | 能进入扫描配置页 |
| `TC-GUI-003` | 新建单文件扫描 | 能选择 `.c/.cpp/.h/.hpp` |
| `TC-GUI-004` | 新建多文件扫描 | 文件列表显示正确 |
| `TC-GUI-005` | 配置 include 路径 | 能添加、删除、保存 |
| `TC-GUI-006` | 配置 Qt 路径 | 能选择 Qt root 和模块 |
| `TC-GUI-007` | 启动扫描 | 进入扫描进度页 |
| `TC-GUI-008` | 暂停/继续扫描 | 状态正确变化 |
| `TC-GUI-009` | 取消扫描 | 生成 Cancelled 报告 |
| `TC-GUI-010` | 查看结果 | 问题列表和详情可展示 |
| `TC-GUI-011` | 报告预览 | WebView2 打开 `report.html` |
| `TC-GUI-012` | 标记误报 | suppression 写入成功 |
| `TC-GUI-013` | 更新基线 | baseline 更新成功 |
| `TC-GUI-014` | 规则管理关闭规则 | 必须填写原因，高风险必须确认 |
| `TC-GUI-015` | 导出 Excel | 生成 `.xlsx` |

### 34.13 release 验收测试用例

| 编号 | 用例 | 预期 |
|---|---|---|
| `TC-REL-001` | 运行 `build-release.ps1` | 生成完整 release |
| `TC-REL-002` | 运行 `check-release.ps1` | 必需项均为 OK |
| `TC-REL-003` | release 移动到新目录 | 仍可运行 |
| `TC-REL-004` | release 放到中文路径 | 仍可运行 |
| `TC-REL-005` | release 放到含空格路径 | 仍可运行 |
| `TC-REL-006` | 无管理员权限运行 | 可启动和扫描 |
| `TC-REL-007` | 缺失 lizard.exe | 首页和 validate 显示错误 |

### 34.14 第一版总体验收标准

第一版完成时，必须满足：

```text
1. Windows 10 可运行；
2. release 目录复制即用；
3. 支持目录、单文件、多文件扫描；
4. 支持显式扫描 .h/.hpp/.hh/.hxx；
5. 支持 C11 和 C++14；
6. 支持 include 路径和宏定义配置；
7. 支持 .vcxproj/.pro 基础读取；
8. 内置 clang-tidy、cppcheck、lizard；
9. 默认使用 default 规则集；
10. 规则库包含 100 条规则；
11. 支持规则关闭，并记录关闭原因和风险确认；
12. 支持首次自动创建 baseline；
13. 支持 New / Existing / Fixed / NotCompared；
14. 支持误报抑制；
15. 支持质量评分 100 分制；
16. 支持复杂度和函数长度指标；
17. 支持 HTML、SARIF、JSON；
18. 支持可选导出 xlsx；
19. 扫描失败文件显示在报告中；
20. 中文路径和空格路径可用。
```

### 34.15 第一版不通过条件

出现以下任一情况，第一版不应验收通过：

```text
release 移动路径后无法运行；
无法扫描单个文件；
无法显式扫描头文件；
无法生成 report.json；
无法生成 HTML 报告；
baseline 首次不能自动创建；
suppression 不能生效；
lizard 复杂度规则不能产生问题；
扫描失败文件没有显示；
中文路径或空格路径导致工具崩溃；
规则关闭不要求填写原因；
高风险规则关闭不要求确认风险。
```

### 34.16 下一步建议

当前设计文档已经覆盖第一版主要方案。

后续可以进入实施阶段：

```text
1. 创建 CodeCheck.sln；
2. 创建 src/CodeCheck.Core；
3. 创建 src/CodeCheck.Cli；
4. 创建 src/CodeCheck.Reporting；
5. 创建 src/CodeCheck.Desktop；
6. 创建 tests/CodeCheck.Tests；
7. 创建 rules/configs/report-templates/scripts/samples 目录；
8. 落地 default-codecheck.json；
9. 落地规则库 JSON 初稿；
10. 实现 CLI validate 和 scan 最小闭环。
```

---

## 35. 第一版实施前最终检查清单

本节作为进入编码实施前的最后检查清单，用于确认第一版范围、目录结构、配置、依赖、风险和优先级已经明确。

### 35.1 范围确认

第一版必须做：

```text
Windows 10 桌面端；
C# WPF + WebView2；
.NET 8 CLI 扫描内核；
目录、单文件、多文件扫描；
显式头文件扫描；
C11 和 C++14；
clang-tidy、cppcheck、lizard；
100 条规则；
default 规则集；
report.json；
report.html；
report.sarif；
report.xlsx 可选导出；
baseline.json；
suppression.json；
release 目录复制即用。
```

第一版明确不做：

```text
安装包；
自动更新；
服务器端；
权限管理；
多人项目管理；
邮件通知；
CI/CD 集成；
SonarQube 本体；
完整自研 C/C++ 编译前端；
源码注释抑制。
```

### 35.2 工程创建清单

需要创建：

```text
CodeCheck.sln
src/CodeCheck.Core
src/CodeCheck.Cli
src/CodeCheck.Reporting
src/CodeCheck.Desktop
tests/CodeCheck.Tests
rules
configs
report-templates
scripts
samples
release
docs
```

项目类型建议：

| 项目 | 类型 | 目标框架 |
|---|---|---|
| `CodeCheck.Core` | Class Library | `net8.0` |
| `CodeCheck.Cli` | Console App | `net8.0` |
| `CodeCheck.Reporting` | Class Library | `net8.0` |
| `CodeCheck.Desktop` | WPF App | `net8.0-windows` |
| `CodeCheck.Tests` | Test Project | `net8.0` |

### 35.3 第一批落地文件清单

第一批建议先落地：

```text
configs/default-codecheck.json
rules/rules.index.json
rules/quectel-c-rules.json
rules/quectel-cpp-rules.json
rules/cert-c-rules.json
rules/cert-cpp-rules.json
rules/rule-mapping.json
rules/rule-profiles.json
scripts/build-release.ps1
scripts/check-release.ps1
scripts/clean-release.ps1
samples/c-demo/README.md
samples/cpp-demo/README.md
samples/qt-demo/README.md
```

### 35.4 第一批代码模块清单

第一批代码应优先实现 CLI 最小闭环。

`CodeCheck.Core`：

```text
Configuration/CodeCheckConfig.cs
Configuration/ConfigLoader.cs
Configuration/ConfigValidator.cs
Inputs/FileDiscoveryService.cs
Inputs/ScanInputFile.cs
Reports/ScanReport.cs
Issues/Issue.cs
Runtime/PathResolver.cs
Runtime/ToolLocator.cs
```

`CodeCheck.Cli`：

```text
Program.cs
Commands/ValidateCommand.cs
Commands/ScanCommand.cs
Services/ScanOrchestrator.cs
Services/CliProgressReporter.cs
```

`CodeCheck.Reporting`：

```text
Json/ReportJsonWriter.cs
Html/HtmlReportWriter.cs
Sarif/SarifReportWriter.cs
```

`CodeCheck.Desktop`：

```text
App.xaml
MainWindow.xaml
ViewModels/MainViewModel.cs
Views/HomeView.xaml
Services/ToolHealthCheckService.cs
```

### 35.5 最小闭环目标

第一轮实现只要求：

```text
CLI 能 validate；
CLI 能 scan；
能读取 default-codecheck.json；
能发现目录、单文件、多文件；
能识别 .c/.cpp/.h/.hpp；
能应用默认排除目录；
能生成基础 report.json；
能生成基础 scan.log；
Desktop 能启动；
Desktop 首页能显示工具状态。
```

暂时可先不接入：

```text
clang-tidy；
cppcheck；
lizard；
baseline；
suppression；
HTML/SARIF/XLSX 完整生成。
```

这些在后续里程碑逐步接入。

### 35.6 配置校验优先级

`validate` 第一批必须校验：

```text
配置文件是否存在；
JSON 是否可解析；
version 是否存在；
rules.ruleIndex 是否存在；
rules.profile 是否存在；
engines 中启用工具的路径是否存在；
report.outputDirectory 是否可创建；
runtime.tempDirectory 是否可创建；
runtime.logDirectory 是否可创建。
```

`scan` 第一批必须额外校验：

```text
input.paths 或 input.fileList 不为空；
输入路径存在；
能发现至少一个可扫描文件；
如果 requireCompileContext=true 且没有 include/project 信息，应输出 ConfigError。
```

### 35.7 文件发现优先级

第一批支持：

```text
.c
.cc
.cpp
.cxx
.h
.hh
.hpp
.hxx
```

目录扫描默认：

```text
扫描 .c/.cc/.cpp/.cxx；
不把头文件作为入口；
排除 third_party、build、out、bin、obj、Debug、Release、.git、.svn、.vs；
排除 moc_*.cpp、ui_*.h、qrc_*.cpp、mocs_compilation.cpp、*_autogen。
```

显式选择头文件时：

```text
允许扫描 .h/.hh/.hpp/.hxx。
```

### 35.8 报告最小字段

第一批 `report.json` 至少包含：

```text
schemaVersion
reportId
tool
project
scan
summary
issues
failedFiles
outputs
logs
```

`issues` 第一批可以为空。

原因：

```text
M2 阶段目标是扫描内核最小闭环，不要求第三方工具已产生真实问题。
```

### 35.9 构建和运行检查

每个阶段完成后必须检查：

```text
dotnet build 成功；
CodeCheck.Cli --version 可运行；
CodeCheck.Cli validate --config configs/default-codecheck.json 可运行；
CodeCheck.Cli scan --config temp/current-codecheck.json 可运行；
CodeCheck.Desktop.exe 可启动；
release 目录可以生成。
```

### 35.10 风险清单

第一版主要风险：

| 风险 | 影响 | 缓解 |
|---|---|---|
| `clang-tidy` 对 include 依赖强 | 扫描失败多 | 不允许静默降级，失败文件入报告 |
| Qt 工程 include 推导不完整 | Qt 文件扫描失败 | 支持手动配置 Qt root |
| Cppcheck/Lizard 输出格式差异 | 解析失败 | 固定工具版本 |
| 规则过多导致误报 | 用户不信任 | 第一版低误报优先 |
| WebView2 缺失 | HTML 预览不可用 | 首页自检并提示 |
| release 路径含中文/空格 | 外部工具调用失败 | 使用 `ProcessStartInfo.ArgumentList` |

### 35.11 开始实施建议顺序

建议接下来按以下顺序实施：

```text
1. 创建解决方案和项目；
2. 创建基础目录；
3. 落地 default-codecheck.json；
4. 落地 rules.index.json 和 rule-profiles.json 空壳；
5. 实现 CodeCheck.Core 配置模型；
6. 实现 ConfigLoader；
7. 实现 ConfigValidator；
8. 实现 CLI --version；
9. 实现 CLI validate；
10. 实现 FileDiscoveryService；
11. 实现基础 ScanOrchestrator；
12. 生成基础 report.json；
13. 生成基础 scan.log；
14. 创建 WPF 空窗口；
15. 创建 release 目录脚本。
```

### 35.12 下一步

下一步可以正式开始创建工程骨架和基础文件。
