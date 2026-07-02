$ErrorActionPreference = "Stop"

function New-Rule($Id, $Title, $Source, $Language, $Severity, $Detection, $DefaultEnabled = $true) {
    [ordered]@{
        id = $Id
        title = $Title
        source = $Source
        sourceRuleId = ""
        language = @($Language)
        severity = $Severity
        detection = $Detection
        defaultEnabled = $DefaultEnabled
        allowDisable = $true
        disableRisk = "关闭该规则后可能漏检相关代码质量或安全问题。"
        description = $Title
        suggestion = "请参考规则说明修改代码。"
    }
}

function Write-RuleFile($Path, $Source, $Rules) {
    [ordered]@{
        schemaVersion = "1.0.0"
        source = $Source
        rules = $Rules
    } | ConvertTo-Json -Depth 8 | Set-Content -Path $Path -Encoding UTF8
}

$cTitles = @(
    "禁止使用 gets 等无边界输入函数",
    "禁止使用不限制长度的字符串拷贝函数",
    "禁止使用不限制长度的格式化输出函数",
    "使用数组时必须保证下标边界有效",
    "指针使用前必须判空或保证有效",
    "局部变量必须初始化后再使用",
    "动态内存申请结果必须检查",
    "动态申请的内存必须在所有路径释放",
    "禁止重复释放同一内存资源",
    "禁止释放后继续访问内存",
    "文件、句柄、锁等资源申请后必须释放",
    "函数返回值应表达明确错误状态",
    "禁止忽略关键库函数返回值",
    "switch 语句应包含 default 分支",
    "case 分支需要明确 break 或注释说明贯穿",
    "宏定义应使用全大写和下划线命名",
    "宏参数和宏整体表达式应使用括号保护",
    "禁止在头文件中定义可导致重复定义的全局对象",
    "函数参数数量不宜过多",
    "函数嵌套层级不宜过深",
    "函数圈复杂度不得超过 10",
    "函数体代码行数不得超过 100 行"
)

$cppTitles = @(
    "头文件中禁止使用 using namespace",
    "头文件中禁止定义非 inline 普通函数",
    "头文件应具有防重复包含保护",
    "禁止在头文件中定义非必要全局变量",
    "类存在虚函数时析构函数应为 virtual",
    "禁止通过非虚析构基类指针删除派生对象",
    "重写虚函数应使用 override",
    "禁止在构造和析构函数中调用虚函数",
    "拥有资源的类应遵循拷贝控制规则",
    "禁止裸 new/delete 管理资源，优先使用 RAII",
    "禁止内存申请后未释放",
    "禁止释放后继续访问对象",
    "禁止重复释放对象或资源",
    "指针解引用前必须保证有效",
    "局部对象和变量应初始化后使用",
    "避免 C 风格强制类型转换",
    "避免不安全的 reinterpret_cast",
    "优先使用 nullptr，不使用 NULL 或 0 表示空指针",
    "可不修改对象状态的成员函数应声明为 const",
    "可不修改的变量和参数应使用 const",
    "禁止忽略关键函数返回值",
    "switch 语句应包含 default 分支",
    "case 分支需要明确 break 或注释说明贯穿",
    "禁止捕获异常后完全忽略",
    "析构函数不应抛出异常",
    "禁止使用已弃用或危险 C 库函数",
    "宏定义应避免替代类型安全的常量或函数",
    "枚举、类、函数、变量命名应符合统一风格",
    "函数参数数量不宜过多",
    "函数嵌套层级不宜过深",
    "禁止大段注释掉的无效代码",
    "单行代码长度不宜过长",
    "包含顺序应稳定，避免无用头文件",
    "禁止循环中进行明显低效的重复计算",
    "禁止返回局部对象的指针或引用",
    "禁止对象切片导致多态信息丢失",
    "函数圈复杂度不得超过 10",
    "函数体代码行数不得超过 100 行"
)

$certCTitles = @(
    "字符串转换整数时必须检查转换错误",
    "不应忽略错误指示或异常返回值",
    "禁止读取未初始化对象",
    "不得越界访问数组",
    "字符串必须以空字符正确终止",
    "字符串拷贝必须保证目标空间足够",
    "禁止使用已释放内存",
    "动态分配的内存只释放一次",
    "动态内存申请结果必须检查",
    "禁止对无效指针执行指针运算",
    "禁止除零和取模零",
    "整数转换不得导致截断或符号错误",
    "整数运算应避免溢出风险",
    "格式化输入输出参数类型必须匹配",
    "禁止使用不受信任的格式化字符串",
    "文件操作后必须检查错误状态",
    "信号处理函数中只能调用异步信号安全函数",
    "多线程共享数据必须受同步保护",
    "禁止使用危险临时文件创建方式",
    "禁止返回局部对象地址"
)

$certCppTitles = @(
    "基类析构函数应支持多态删除",
    "拷贝构造和拷贝赋值应正确处理自赋值",
    "析构函数不得抛出异常",
    "构造函数中资源获取失败应保持对象状态安全",
    "禁止访问生命周期已结束的对象",
    "动态分配内存必须正确释放",
    "禁止使用不匹配的分配和释放形式",
    "禁止空指针解引用",
    "表达式求值不应依赖未指定顺序",
    "不应忽略具有错误含义的返回值",
    "异常处理不应捕获后完全忽略",
    "禁止从析构函数中逃逸异常",
    "禁止返回局部对象的引用或指针",
    "禁止对象切片破坏多态语义",
    "禁止越界访问容器或数组",
    "迭代器使用前必须保证有效",
    "禁止对无效迭代器执行解引用",
    "类型转换不得破坏对象有效类型",
    "多线程共享对象访问必须同步",
    "禁止使用危险或过时的库接口"
)

$cRules = for ($i = 1; $i -le 22; $i++) {
    $severity = if ($i -in 1,9,10) { "Blocker" } elseif ($i -le 11) { "Critical" } elseif ($i -in 16,19) { "Suggestion" } else { "Warning" }
    $detection = if ($i -eq 12) { "manual" } elseif ($i -in 21,22) { "lizard" } else { "builtin" }
    New-Rule ("Quectel-C-{0:D3}" -f $i) $cTitles[$i - 1] "Huawei-C" "c" $severity $detection ($detection -ne "manual")
}

$cppRules = for ($i = 1; $i -le 38; $i++) {
    $severity = if ($i -in 12,13,35) { "Blocker" } elseif ($i -in 5,6,8,9,11,14,15,17,25,26) { "Critical" } elseif ($i -in 7,18,19,20,27,28,29,31,32,33,34) { "Suggestion" } else { "Warning" }
    $detection = if ($i -in 37,38) { "lizard" } else { "clang-tidy" }
    New-Rule ("Quectel-CPP-{0:D3}" -f $i) $cppTitles[$i - 1] "Huawei-CPP" "cpp" $severity $detection
}

$certCRules = for ($i = 1; $i -le 20; $i++) {
    $severity = if ($i -in 4,7,8,11,20) { "Blocker" } elseif ($i -in 1,3,5,6,9,10,14,15) { "Critical" } else { "Warning" }
    $detection = if ($i -in 17,18) { "manual" } else { "clang-tidy" }
    New-Rule ("Quectel-CERT-C-{0:D3}" -f $i) $certCTitles[$i - 1] "CERT-C" "c" $severity $detection ($detection -ne "manual")
}

$certCppRules = for ($i = 1; $i -le 20; $i++) {
    $severity = if ($i -in 5,8,13) { "Blocker" } elseif ($i -in 1,3,6,7,12,15,16,17,18) { "Critical" } else { "Warning" }
    $detection = if ($i -in 4,19) { "manual" } else { "clang-tidy" }
    New-Rule ("Quectel-CERT-CPP-{0:D3}" -f $i) $certCppTitles[$i - 1] "CERT-CPP" "cpp" $severity $detection ($detection -ne "manual")
}

Write-RuleFile "rules/quectel-c-rules.json" "Huawei-C" $cRules
Write-RuleFile "rules/quectel-cpp-rules.json" "Huawei-CPP" $cppRules
Write-RuleFile "rules/cert-c-rules.json" "CERT-C" $certCRules
Write-RuleFile "rules/cert-cpp-rules.json" "CERT-CPP" $certCppRules

$allIds = @($cRules + $cppRules + $certCRules + $certCppRules | ForEach-Object { $_.id })
[ordered]@{
    schemaVersion = "1.0.0"
    profiles = @(
        [ordered]@{
            name = "default"
            description = "第一版默认规则集"
            enabledRuleIds = $allIds
        }
    )
} | ConvertTo-Json -Depth 8 | Set-Content -Path "rules/rule-profiles.json" -Encoding UTF8

Write-Host "Generated $($allIds.Count) rules."
