# RuntimeCoroutineTracker

## Features

*(to be added)*

## Journals

[2016-09-22] 功能改进

- 支持插件内 (Plugins) 的协程追踪 (新增类 CoroutinePluginForwarder)
- 处理 Profiler 输出时的乱码问题

[2016-09-20] 增加 `RuntimeCoroutineTracker` 类，对协程的行为进行追踪和记录

- **性能分析** 目前支持对每一个协程的每一次 yield 过程执行 `BeginSample()`/ `EndSample()` 
- **统计输出** 可以知道程序任意时间点上运行着多少活动的协程

如果希望新写出来的协程可以被追踪，需要把普通形式的协程调用

```cs
abc.StartCoroutine(xxx());
```

使用如下的调用方式替换

```cs
RuntimeCoroutineTracker.InvokeStart(abc, xxx());
```

注意这两种调用方式在未开启追踪的情况下的行为完全一致。

