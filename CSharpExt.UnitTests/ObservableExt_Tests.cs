using Microsoft.Reactive.Testing;
using ReactiveUI.Testing;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Xunit;
using Noggog;
using System.Threading.Tasks;
using FluentAssertions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Noggog.Utility;
using Path = System.IO.Path;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.Xunit2;
using DynamicData;
using Noggog.Testing.AutoFixture;
using Noggog.Testing.FileSystem;
#if NETSTANDARD2_0 
using TaskCompletionSource = Noggog.TaskCompletionSource;
#else 
using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource;
#endif

namespace CSharpExt.UnitTests
{
    public class ObservableExt_Tests
    {
        [Fact]
        public async Task SelectReplaceAsync() => await new TestScheduler().WithAsync(async scheduler =>
        {
            const int longInitialWait = 10000;
            const int secondWait = 1000;
            List<int> results = new List<int>();
            Subject<int> waitSubject = new Subject<int>();
            int throwCount = 0;
            TaskCompletionSource complete = new TaskCompletionSource();
            waitSubject
                .SelectReplace(async (i, c) =>
                {
                    try
                    {
                        await Observable.Timer(TimeSpan.FromTicks(i), scheduler).ToTask(c).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        throwCount++;
                        return -1;
                    }
                    results.Add(i);
                    return i;
                })
                .Subscribe(
                    onNext: (i) => { },
                    onCompleted: () => complete.SetResult());
            waitSubject.OnNext(longInitialWait);
            scheduler.AdvanceBy(longInitialWait / 2);
            waitSubject.OnNext(secondWait);
            waitSubject.OnCompleted();
            throwCount.Should().Equals(1);
            scheduler.AdvanceBy(secondWait - 10);
            results.Should().HaveCount(0);
            scheduler.AdvanceBy(20);
            await complete.Task;
            results.Should().HaveCount(1);
            results.Should().ContainInOrder(secondWait);
        });

        [Theory, TestData]
        public void WatchFile_Typical(
            [Frozen]FilePath path,
            [Frozen]MockFileSystemWatcher mockFileWatcher,
            [Frozen]MockFileSystem fs)
        {
            int count = 0;
            using var sub = ObservableExt.WatchFile(path, fileWatcherFactory: fs.FileSystemWatcher)
                .Subscribe(x => count++);
            count.Should().Be(0);
            fs.File.WriteAllText(path, string.Empty);
            mockFileWatcher.MarkCreated(path);
            count.Should().Be(1);
        }

        [Theory, TestData]
        public void WatchFile_AtypicalPathSeparators(
            [Frozen]FilePath path,
            [Frozen]MockFileSystemWatcher mockFileWatcher,
            [Frozen]MockFileSystem fs)
        {
            int count = 0;
            using var sub = ObservableExt.WatchFile(path.Path.Replace('\\', '/'), fileWatcherFactory: fs.FileSystemWatcher)
                .Subscribe(x => count++);
            count.Should().Be(0);
            fs.File.WriteAllText(path, string.Empty);
            mockFileWatcher.MarkCreated(path);
            count.Should().Be(1);
        }

        [Theory, TestData]
        public void WatchFolder_Typical(
            [Frozen]DirectoryPath dir,
            [Frozen]MockFileSystemWatcher mockFileWatcher,
            [Frozen]MockFileSystem fs)
        {
            FilePath fileA = Path.Combine(dir.Path, "FileA");
            FilePath fileB = Path.Combine(dir.Path, "FileB");
            fs.File.WriteAllText(fileA, string.Empty);
            var live = ObservableExt.WatchFolderContents(dir.Path, fileSystem: fs)
                .RemoveKey();
            var list = live.AsObservableList();
            list.Count.Should().Be(1);
            list.Items.ToExtendedList()[0].Should().Be(fileA);
            fs.File.WriteAllText(fileB, string.Empty);
            mockFileWatcher.MarkCreated(fileB);
            list = live.AsObservableList();
            list.Count.Should().Be(2);
            list.Items.ToExtendedList()[0].Should().Be(fileA);
            list.Items.ToExtendedList()[1].Should().Be(fileB);
            fs.File.Delete(fileA);
            mockFileWatcher.MarkDeleted(fileA);
            list = live.AsObservableList();
            list.Count.Should().Be(1);
            list.Items.ToExtendedList()[0].Should().Be(fileB);
        }

        [Theory, TestData]
        public void WatchFolder_OnlySubfolder(
            [Frozen]DirectoryPath dir,
            [Frozen]MockFileSystemWatcher mockFileWatcher,
            [Frozen]MockFileSystem fs)
        {
            FilePath fileA = Path.Combine(dir.Path, "SomeFolder", "FileA");
            FilePath fileB = Path.Combine(dir.Path, "FileB");
            fs.Directory.CreateDirectory(Path.GetDirectoryName(fileA)!);
            fs.File.WriteAllText(fileA, string.Empty);
            var live = ObservableExt.WatchFolderContents(Path.Combine(dir.Path, "SomeFolder"), fileSystem: fs)
                .RemoveKey();
            var list = live.AsObservableList();
            list.Count.Should().Be(1);
            list.Items.ToExtendedList()[0].Should().Be(fileA);
            fs.File.WriteAllText(fileB, string.Empty);
            mockFileWatcher.MarkCreated(fileB);
            list = live.AsObservableList();
            list.Count.Should().Be(1);
            list.Items.ToExtendedList()[0].Should().Be(fileA);
        }

        [Theory, TestData]
        public void WatchFolder_ATypicalSeparator(
            [Frozen]DirectoryPath dir,
            [Frozen]FilePath file,
            [Frozen]MockFileSystemWatcher mockFileWatcher,
            [Frozen]MockFileSystem fs)
        {
            var live = ObservableExt.WatchFolderContents(dir.Path.Replace('\\', '/'), fileSystem: fs)
                .RemoveKey();
            var list = live.AsObservableList();
            list.Count.Should().Be(0);
            fs.File.WriteAllText(file, string.Empty);
            mockFileWatcher.MarkCreated(file);
            list.Count.Should().Be(1);
            list.Items.ToExtendedList()[0].Should().Be(file);
        }
    }
}
