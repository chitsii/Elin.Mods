using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using UnityEngine;

public static class RuntimeV2Config
{
    public static readonly string ResultPath = @"__RESULT_PATH__";
    public static readonly string RequiredNameContains = "__REQUIRED_NAME_CONTAINS__";
    public static readonly string ModRoot = @"__MOD_ROOT__";
    public static readonly string CaseIdFilter = "__CASE_ID_FILTER__";
    public static readonly string TagFilter = "__TAG_FILTER__";
}

// ---- generated sources ----
__SOURCE_BLOCK__
// ---- generated sources end ----

var runnerObject = new GameObject("ArsRuntimeTestRunnerV2");
runnerObject.AddComponent<__ENTRYPOINT_CLASS__>();
