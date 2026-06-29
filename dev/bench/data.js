window.BENCHMARK_DATA = {
  "lastUpdate": 1782750784812,
  "repoUrl": "https://github.com/Chris-Wolfgang/EF-Audit",
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
      }
    ]
  }
}