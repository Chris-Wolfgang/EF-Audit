window.BENCHMARK_DATA = {
  "lastUpdate": 1782783967118,
  "repoUrl": "https://github.com/Chris-Wolfgang/AuditTrail",
  "entries": {
    "Audit Interceptor Benchmarks": [
      {
        "commit": {
          "author": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "79bee9990585e287a53cca1155161a11935cc62d",
          "message": "Merge pull request #140 from Chris-Wolfgang/initial-dev\n\nRelease v0.1.0 — initial-dev → main",
          "timestamp": "2026-06-29T12:31:18-04:00",
          "tree_id": "d8434a3bf0fea98f6278920de91cf1395f6a3dcb",
          "url": "https://github.com/Chris-Wolfgang/EF-Audit/commit/79bee9990585e287a53cca1155161a11935cc62d"
        },
        "date": 1782750783969,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 1)",
            "value": 556003.9736842106,
            "unit": "ns",
            "range": "± 12339.43211537"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 1)",
            "value": 1676643.6172839506,
            "unit": "ns",
            "range": "± 165983.60342715844"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 1)",
            "value": 915243.9285714285,
            "unit": "ns",
            "range": "± 80086.05684819337"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 1)",
            "value": 2597533.1,
            "unit": "ns",
            "range": "± 42945.27835812853"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 1)",
            "value": 771607.9367816092,
            "unit": "ns",
            "range": "± 42134.15834933699"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 1)",
            "value": 1630335,
            "unit": "ns",
            "range": "± 24504.857250851623"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 10)",
            "value": 1347041.2307692308,
            "unit": "ns",
            "range": "± 12414.76914910789"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 10)",
            "value": 9381608.782828283,
            "unit": "ns",
            "range": "± 760818.1798455332"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 10)",
            "value": 2678452.1153846155,
            "unit": "ns",
            "range": "± 35407.60104492269"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 10)",
            "value": 18356478.73,
            "unit": "ns",
            "range": "± 4597397.050309221"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 10)",
            "value": 2130068,
            "unit": "ns",
            "range": "± 21918.631399532485"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 10)",
            "value": 13220713.088607594,
            "unit": "ns",
            "range": "± 667661.0869320504"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 50)",
            "value": 6868407.153061224,
            "unit": "ns",
            "range": "± 724891.3287870584"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 50)",
            "value": 24169084.14,
            "unit": "ns",
            "range": "± 14464137.882429594"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 50)",
            "value": 12934152.121212121,
            "unit": "ns",
            "range": "± 1326624.41079268"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 50)",
            "value": 18503679.5125,
            "unit": "ns",
            "range": "± 3491320.452630008"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 50)",
            "value": 9148299.742424242,
            "unit": "ns",
            "range": "± 1535684.734449226"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 50)",
            "value": 21201899.752873562,
            "unit": "ns",
            "range": "± 11164461.948134204"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9d2fa03ebee16d9878f7691abb5d5627cbbb5a34",
          "message": "Merge pull request #153 from Chris-Wolfgang/dependabot/github_actions/github-actions-72aec035ae\n\nBump the github-actions group with 8 updates",
          "timestamp": "2026-06-29T13:29:38-04:00",
          "tree_id": "2b1242d7833ed28522227288e0f94a4b89196d96",
          "url": "https://github.com/Chris-Wolfgang/EF-Audit/commit/9d2fa03ebee16d9878f7691abb5d5627cbbb5a34"
        },
        "date": 1782754287589,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 1)",
            "value": 620840.1894736842,
            "unit": "ns",
            "range": "± 50160.159529102304"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 1)",
            "value": 1513572.4666666666,
            "unit": "ns",
            "range": "± 17694.224501097473"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 1)",
            "value": 830047.5333333333,
            "unit": "ns",
            "range": "± 11821.274942520653"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 1)",
            "value": 2937814.1666666665,
            "unit": "ns",
            "range": "± 25559.524774474383"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 1)",
            "value": 708737.9333333333,
            "unit": "ns",
            "range": "± 12319.46246199001"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 1)",
            "value": 1816481.6666666667,
            "unit": "ns",
            "range": "± 15039.3332283177"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 10)",
            "value": 1655569.8666666667,
            "unit": "ns",
            "range": "± 27816.43828454849"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 10)",
            "value": 10886038.335051546,
            "unit": "ns",
            "range": "± 726153.4666902188"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 10)",
            "value": 3455502.0714285714,
            "unit": "ns",
            "range": "± 26475.828411428953"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 10)",
            "value": 20745164.07,
            "unit": "ns",
            "range": "± 6524799.646514701"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 10)",
            "value": 2632454.1,
            "unit": "ns",
            "range": "± 38752.20405860807"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 10)",
            "value": 16312479.540816326,
            "unit": "ns",
            "range": "± 3701654.1463870546"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 50)",
            "value": 6808001.923076923,
            "unit": "ns",
            "range": "± 26340.49618446072"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 50)",
            "value": 25873701.555555556,
            "unit": "ns",
            "range": "± 15303201.873512555"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 50)",
            "value": 17896555.2,
            "unit": "ns",
            "range": "± 299225.13166372146"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 50)",
            "value": 23525245.49382716,
            "unit": "ns",
            "range": "± 6456502.106865335"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 50)",
            "value": 14053217.85,
            "unit": "ns",
            "range": "± 323054.9354847896"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 50)",
            "value": 21634352.234939758,
            "unit": "ns",
            "range": "± 5446128.945774243"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "160a6175104e5e178be78e58c219f7aff4090cb5",
          "message": "Merge pull request #156 from Chris-Wolfgang/release-prep/v0.1.0-readiness\n\nrelease prep: trusted publishing + exclude Cli from NuGet (v0.1.0 readiness)",
          "timestamp": "2026-06-29T14:38:15-04:00",
          "tree_id": "ab4775ddc8082e48cf095ebc5178457456dbf665",
          "url": "https://github.com/Chris-Wolfgang/EF-Audit/commit/160a6175104e5e178be78e58c219f7aff4090cb5"
        },
        "date": 1782758423816,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 1)",
            "value": 772722.5306122449,
            "unit": "ns",
            "range": "± 228039.79841089706"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 1)",
            "value": 1982694.3789473684,
            "unit": "ns",
            "range": "± 191472.96051257636"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 1)",
            "value": 1001249.2608695652,
            "unit": "ns",
            "range": "± 86396.44745601008"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 1)",
            "value": 3151777.785714286,
            "unit": "ns",
            "range": "± 40481.36673245448"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 1)",
            "value": 845712.5824175824,
            "unit": "ns",
            "range": "± 68896.59255226325"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 1)",
            "value": 2223065.066666667,
            "unit": "ns",
            "range": "± 162469.12979752195"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 10)",
            "value": 1714236.6923076923,
            "unit": "ns",
            "range": "± 22270.20621587137"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 10)",
            "value": 11008205.403225806,
            "unit": "ns",
            "range": "± 727437.3978567574"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 10)",
            "value": 4212558.543956044,
            "unit": "ns",
            "range": "± 292583.46749063337"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 10)",
            "value": 20164772.76,
            "unit": "ns",
            "range": "± 6367152.867082722"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 10)",
            "value": 3385578.536082474,
            "unit": "ns",
            "range": "± 295563.20373896125"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 10)",
            "value": 15701979.86,
            "unit": "ns",
            "range": "± 2292030.434976011"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 50)",
            "value": 7043426.785714285,
            "unit": "ns",
            "range": "± 73102.23850111313"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 50)",
            "value": 21038894.489130434,
            "unit": "ns",
            "range": "± 11079515.311085826"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 50)",
            "value": 16663554.707070706,
            "unit": "ns",
            "range": "± 2448016.181724326"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 50)",
            "value": 23557198.61728395,
            "unit": "ns",
            "range": "± 5043578.917716474"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 50)",
            "value": 11115877.69,
            "unit": "ns",
            "range": "± 2902114.4926090403"
          },
          {
            "name": "Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 50)",
            "value": 23658123.03448276,
            "unit": "ns",
            "range": "± 7631549.76965058"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "441668c7b681d4caad9fa8c0a9f88fcefb66d058",
          "message": "Merge pull request #157 from Chris-Wolfgang/rename/audittrail\n\nrename: Wolfgang.Audit.* → Wolfgang.AuditTrail.* (pre-v0.1.0)",
          "timestamp": "2026-06-29T17:13:35-04:00",
          "tree_id": "7cefdf6862f441389874812848b90994f73718eb",
          "url": "https://github.com/Chris-Wolfgang/EF-Audit/commit/441668c7b681d4caad9fa8c0a9f88fcefb66d058"
        },
        "date": 1782767721317,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 1)",
            "value": 557097.3684210526,
            "unit": "ns",
            "range": "± 12146.951127342963"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 1)",
            "value": 1521961.5,
            "unit": "ns",
            "range": "± 25837.454807373673"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 1)",
            "value": 852259,
            "unit": "ns",
            "range": "± 14228.996276563626"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 1)",
            "value": 3048954.8571428573,
            "unit": "ns",
            "range": "± 32084.849129617698"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 1)",
            "value": 716070.0476190476,
            "unit": "ns",
            "range": "± 16921.566143463762"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 1)",
            "value": 2164016.7802197803,
            "unit": "ns",
            "range": "± 155319.54056001254"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 10)",
            "value": 1679141.0666666667,
            "unit": "ns",
            "range": "± 12443.847484179636"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 10)",
            "value": 12031099.042553192,
            "unit": "ns",
            "range": "± 1234169.0673702739"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 10)",
            "value": 3426245.1153846155,
            "unit": "ns",
            "range": "± 18878.145462140703"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 10)",
            "value": 23749614.016666666,
            "unit": "ns",
            "range": "± 1057806.0327043957"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 10)",
            "value": 2686604,
            "unit": "ns",
            "range": "± 24726.746394799444"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 10)",
            "value": 17659057.66,
            "unit": "ns",
            "range": "± 3290318.697734289"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 50)",
            "value": 8537646.622448979,
            "unit": "ns",
            "range": "± 741737.4098618926"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 50)",
            "value": 25559102.237373736,
            "unit": "ns",
            "range": "± 14609936.794062324"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 50)",
            "value": 18389528.14285714,
            "unit": "ns",
            "range": "± 306820.8154516266"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 50)",
            "value": 22047663.512658227,
            "unit": "ns",
            "range": "± 2975779.7964042393"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 50)",
            "value": 11651840.82,
            "unit": "ns",
            "range": "± 2946040.278120135"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 50)",
            "value": 22256770.404761903,
            "unit": "ns",
            "range": "± 6430516.6188523825"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16149e16143eb86cd9c45f86b0ca8ef36e1041bb",
          "message": "Merge pull request #160 from Chris-Wolfgang/ci/release-concurrency-group\n\nci(release): add concurrency group so duplicate releases don't race",
          "timestamp": "2026-06-29T21:24:10-04:00",
          "tree_id": "a3c1ed57668b91f0b811ed1d6ceaf8ed1d3034a6",
          "url": "https://github.com/Chris-Wolfgang/AuditTrail/commit/16149e16143eb86cd9c45f86b0ca8ef36e1041bb"
        },
        "date": 1782782751856,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 1)",
            "value": 545362.195652174,
            "unit": "ns",
            "range": "± 13567.737489536721"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 1)",
            "value": 1677677.293478261,
            "unit": "ns",
            "range": "± 162481.31963185346"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 1)",
            "value": 804931.6153846154,
            "unit": "ns",
            "range": "± 11212.212779959043"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 1)",
            "value": 2597776.7333333334,
            "unit": "ns",
            "range": "± 43788.35837862888"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 1)",
            "value": 748948.3092783506,
            "unit": "ns",
            "range": "± 68695.29654998842"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 1)",
            "value": 1569334.2142857143,
            "unit": "ns",
            "range": "± 15746.228877849655"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 10)",
            "value": 1281075.9285714286,
            "unit": "ns",
            "range": "± 15223.958761738711"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 10)",
            "value": 9404299.274193548,
            "unit": "ns",
            "range": "± 940238.6908273222"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 10)",
            "value": 2427546.0714285714,
            "unit": "ns",
            "range": "± 36699.138366461135"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 10)",
            "value": 20294856.28,
            "unit": "ns",
            "range": "± 4049033.290176611"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 10)",
            "value": 1890415.4666666666,
            "unit": "ns",
            "range": "± 18291.62159439072"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 10)",
            "value": 15159218.35,
            "unit": "ns",
            "range": "± 2592073.3349846127"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 50)",
            "value": 4769999.653846154,
            "unit": "ns",
            "range": "± 34211.15948255811"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 50)",
            "value": 26724640.29,
            "unit": "ns",
            "range": "± 15617524.321380239"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 50)",
            "value": 13340588.336734693,
            "unit": "ns",
            "range": "± 1581279.3335644072"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 50)",
            "value": 23297140.467032965,
            "unit": "ns",
            "range": "± 11999938.311296584"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 50)",
            "value": 9587709.823529411,
            "unit": "ns",
            "range": "± 190554.78022022804"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 50)",
            "value": 20803183.023255814,
            "unit": "ns",
            "range": "± 9973806.177505178"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "210299580+Chris-Wolfgang@users.noreply.github.com",
            "name": "Chris Wolfgang",
            "username": "Chris-Wolfgang"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "bf012bca770d7e5e4e49b9d1f08e4fe3a1e537d6",
          "message": "Merge pull request #159 from Chris-Wolfgang/fix/slnx-complete-projects\n\nfix(slnx): add the 4 projects missing from AuditTrail.slnx (unblocks v0.1.0 release)",
          "timestamp": "2026-06-29T21:44:23-04:00",
          "tree_id": "faa944780ff0851a3924324274d50ee7dffc8237",
          "url": "https://github.com/Chris-Wolfgang/AuditTrail/commit/bf012bca770d7e5e4e49b9d1f08e4fe3a1e537d6"
        },
        "date": 1782783966375,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 1)",
            "value": 560513.1666666666,
            "unit": "ns",
            "range": "± 14067.377786819456"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 1)",
            "value": 1531410.2333333334,
            "unit": "ns",
            "range": "± 28277.278954430298"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 1)",
            "value": 835339.2222222222,
            "unit": "ns",
            "range": "± 15343.352167692366"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 1)",
            "value": 2930126.4,
            "unit": "ns",
            "range": "± 25209.47353618137"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 1)",
            "value": 716843,
            "unit": "ns",
            "range": "± 13579.687050885967"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 1)",
            "value": 1830203.3333333333,
            "unit": "ns",
            "range": "± 26013.516949755907"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 10)",
            "value": 1651023.3333333333,
            "unit": "ns",
            "range": "± 30724.709622402494"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 10)",
            "value": 10950889.808080807,
            "unit": "ns",
            "range": "± 904366.0179935463"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 10)",
            "value": 3429223.1,
            "unit": "ns",
            "range": "± 25396.10166316307"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 10)",
            "value": 23135433.804347824,
            "unit": "ns",
            "range": "± 882486.5315026025"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 10)",
            "value": 2621471.433333333,
            "unit": "ns",
            "range": "± 21488.84211620622"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 10)",
            "value": 17157757.21,
            "unit": "ns",
            "range": "± 2306466.7316353233"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_without_audit(BatchSize: 50)",
            "value": 7925536.489795919,
            "unit": "ns",
            "range": "± 702079.8869151932"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Insert_with_audit(BatchSize: 50)",
            "value": 49267054.71428572,
            "unit": "ns",
            "range": "± 1131407.0173404063"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_without_audit(BatchSize: 50)",
            "value": 15886578.616161617,
            "unit": "ns",
            "range": "± 1973858.6686270623"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.Lifecycle_with_audit(BatchSize: 50)",
            "value": 21506479.82278481,
            "unit": "ns",
            "range": "± 3884133.974411238"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_without_audit(BatchSize: 50)",
            "value": 12969924.529411765,
            "unit": "ns",
            "range": "± 258775.74474318436"
          },
          {
            "name": "Wolfgang.AuditTrail.Benchmarks.SaveChangesBenchmarks.MixedStates_per_save_with_audit(BatchSize: 50)",
            "value": 22766824.534883723,
            "unit": "ns",
            "range": "± 7848289.434830349"
          }
        ]
      }
    ]
  }
}