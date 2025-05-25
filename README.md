# MyUnsafeUtil
NativeArrayを使いやすくするためのラッパー。\
NativeArrayをeditor内で使うとうちの環境では普通の配列より二倍遅く\
開発に支障が出たのでラッパーで効率化。

また、managed配列を渡せるようにとか、usingで自動開放できるようにしてみた。\
structではなくclassなので多少オーバーヘッドはあるけれどわりと楽が出来るので便利？

速度のいるところへは[BurstCompile]のメソッドへNativeArrayとして渡し、\
管理はManaged配列のように楽に受け渡したり開放できたりします。

※EditorやDevelopmentBuildでDisposeしないままアプリを終了すると\
メモリを確保した場所のスタックトレースをログに出力します。\
usingを使うか、不要になったらDisposeしましょう！


何か見落としているかもだけれど今のところよく動いてます！

*Unity2022.3でのみ動作確認

# Install
UnityのPackageManagerで"Add package from git URL..."にて以下を指定。\
https://github.com/r-benjamin-cotton/MyUnsafeUtil.git


# Usage
```
using MyUnsafeUtil;
```

```
[BurstCompile]
private static void BurstFunc(in NativeArray<int> buf)
{
  for (int i = 0, end = buf.Length; i < end; i++)
  {
    // todo:
  }
}
private void Sample(MyReadOnlyNativeArray<int> array)
{
  BurstFunc(ref array.NativeArray);
}
public void Xxx()
{
  // メモリをNativeArrayから一時的に確保
  {
    var length = 100;
    using var temp = new MyNativeArray<int>(length, Unity.Collections.Allocator.Temp, Unity.Collections.NativeArrayOptions.UninitializedMemory);
    Sample(temp);
  }
  // メモリをNativeArrayから恒久的に確保
  {
    var length = 100;
    temp = new MyNativeArray<int>(length, false);  <<-- 二つ目のbool値をfalseにすると開放忘れの時にログを出す
    Sample(temp);
    ~~~
    temp.Dispose();
  }
  // ManagedArrayをNativeArray化
  {
    var temp = new marray[100];
    using var temp = new MyNativeArray<int>(marray);
    Sample(temp);
  }
}
```

