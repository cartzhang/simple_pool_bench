
unity商店里面有各种各样的对象池，有smart pool，有pollmanager等各个版本。
本来不太喜欢重复制作轮子，因为个人觉得以解决问题为中心，能用别人的成果，就是很好快捷的方法。

但是这个下面的制作对象池，是团队里的一个同学，最初作为学习unity的一个方法，我们开始一直使用的unity商店里提供的poolmanger的一个版本，但是这个同学很不满意，觉得它的不好用，自己也希望尝试来做一个，于是乎就自己做了一个。
再后来他写完了，让我帮忙看看，我就把它做了一些使用方法上的快捷和整理，对里面的部分代码进行了稍微的修改。

在这里对这位同学表示感谢，辛苦！

## 一、 什么是对象池

在激活对象时，它从池中提取。在停用对象时，它放回池中，等待下一个请求。

当调用对象时，不使用常规的new 构造子的方式，而是通过一个对象池操作。即如果池中存在该对象，则取出；如果不存在，则新建一个对象并存储在池中。当使用完该对象后，则将该对象的归还给对象池。

以上就是对象池的概念。

## 二、代码设计

对于这个simple pool，我们使用了LinkedList作为存放对象的容器的。
LinkedList底层采用链表存储元素，所以对于频繁的增删操作效率很高。
为什么使用LinkedList而不是List，因为之前的Pool 对象池就是使用的List，而它的删除效率是线性的，而linkedlist是常量，对删除和插入速度都比较快。
下面List<T> 和LinkedList<T>消耗时间对比：
#### Append

LinkedList<T>.AddLast(item) constant time

List<T>.Add(item) amortized constant time, linear worst case

#### Prepend

LinkedList<T>.AddFirst(item) constant time

List<T>.Insert(0, item) linear time

#### Insertion

LinkedList<T>.AddBefore(node, item) constant time

LinkedList<T>.AddAfter(node, item) constant time

List<T>.Insert(index, item) linear time

#### Removal

LinkedList<T>.Remove(item) linear time

LinkedList<T>.Remove(node) constant time

List<T>.Remove(item) linear time

List<T>.RemoveAt(index) linear time

#### Count
LinkedList<T>.Count constant time

List<T>.Count constant time

#### Contains
LinkedList<T>.Contains(item) linear time

List<T>.Contains(item) linear time

#### Clear

LinkedList<T>.Clear() linear time

List<T>.Clear() linear time

参考网址：http://stackoverflow.com/questions/169973/when-should-i-use-a-list-vs-a-linkedlist

其实这里选择LinkedList只是因为这个同学觉得List的删除太慢，之前影响过我们，所以它觉得链表应该会好些，效果确实是好了很多。但是不完全是因为使用了链表，而是因为这个对象池的代码里，没有做过多的查询，你可以看到contain，linkedlist和list效果应该差别不大。
使用了只有三个方法：AddLast,Remove,Clear,并且Remove使用的还是Remove(value)，so...。但是对于内存的使用，不需要开辟连续内存是有好处的。因为list的地址是一个，首地址。而linkedlist可以使用其他碎片地址。

## 三、实现代码

总共的实现脚本有三个,在scripts文件夹下；测试脚本两个，在scripts/demo文件夹下。

#### 1. ObjectPool.cs

一开始申请两个链表，一个存放使用对象，一个存放未使用对象。需要从对象池内取，就看看有没有可用的，没有就产生一个，有就取出一个。用完了，需要返回，就直接从使用的列表中删除，转移到未使用的链表中。

实现的代码在ObjectPool.cs文件中。

这个文件中，唯一需要注意是就是延时回池的过程中，可能被其他触发回池，或多次延迟回池，这样可能需要多次回池造成回池失败，还需要查询来判断是否已经回池。即使可以查询是否已经回池，也会有第一次已经回池，延时还没执行到，第二次又取了出去，刚取出没有多久，第一次的延时回池到了，又回池了。这个说起来，挺拗口的，但是在实际中真的有这样的情况。

怎么解决多次回池，我们借鉴或说是直接拿来用的是poolmanager的处理方法。

```

        IEnumerator DelayPushObjectToPool(Transform handleTransform, float delayTime)
        {
            while (delayTime > 0)
            {
                yield return null;
                // If the instance was deactivated while waiting here, just quit
                if (!handleTransform.gameObject.activeInHierarchy)
                {
                    yield break;
                }
                delayTime -= Time.deltaTime;
            }
            PushObjectToPool(handleTransform);
        }

```

这样若延时时间到，或被回池，就看看场景中是否还可见，若不可见就不进行下面的回池处理了，说明已经回到对象池中了。

#### 2.PoolManager.cs

这个文件中，对上面脚步中的调用，进行了封装。
一开始的调用接口为：
PullObjcetFromPool 和 PushObjectPool 这个四个接口，因为有重载。
但是后来觉得别人写的时候，需要添加命名空间，习惯于Unity的Instantiate有使用的差异，所以就做了GameObject的扩展函数。

    `	
	/// <summary>
	/// 扩展GameObject函数
	/// 使用扩展函数方面代码调用，可以Unity的任意代码中，调用对象池来产生和回池对象，
	/// 并且不需要调用命名空间等繁琐东西。
	/// 代码例子： obj = gameObject.InstantiateFromPool(prefab);
	///  gameObject.DestroyToPool(poolObjs[poolObjs.Count - 1], 0.2f);
	///  具体可以参考HowToUse脚本和TestEffect。
	///  @cartzhang
	/// </summary>
	public static class GameObjectExten
	{
	    /// <summary>
	    /// 调用对象池，产生一对象，并带有位置和旋转等参考
	    /// </summary>
	    /// <param name="gameobject"></param>
	    /// <param name="original"></param>
	    /// <param name="position"></param>
	    /// <param name="rotation"></param>
	    /// <returns></returns>
	    public static GameObject InstantiateFromPool(this GameObject gameobject, Object original, Vector3 position, Quaternion rotation)
	    {
	        return PoolManager.PullObjcetFromPool(original as GameObject, position, rotation).gameObject;
	    }
	    /// <summary>
	    /// 调用对象池，产生对象
	    /// </summary>
	    /// <param name="gameobject"></param>
	    /// <param name="original"></param>
	    /// <returns></returns>
	    public static GameObject InstantiateFromPool(this GameObject gameobject, Object original)
	    {
	        return PoolManager.PullObjcetFromPool(original as GameObject).gameObject;
	    }
	    /// <summary>
	    /// 对象返回对象池
	    /// </summary>
	    /// <param name="gameobject"></param>
	    /// <param name="obj"></param>
	    public static void DestroyToPool(this GameObject gameobject, Object obj)
	    {
	        PoolManager.PushObjectPool((obj as GameObject).transform);
	    }
	    /// <summary>
	    /// 带延时的返回对象池
	    /// </summary>
	    /// <param name="gameobject"></param>
	    /// <param name="obj"></param>
	    /// <param name="t"></param>
	    public static void DestroyToPool(this GameObject gameobject, Object obj, [DefaultValue("0.0F")] float t)
	    {
	        PoolManager.PushObjectPool((obj as GameObject).transform, t);
	    }
	}
	`

在其他需要调用的脚本中的调用方法：


```
obj = gameObject.InstantiateFromPool(prefab);
和
 gameObject.DestroyToPool(poolObjs[poolObjs.Count - 1], 0.2f);
```

至于gameobject是那个对象，这里不关紧要，因为只是为了引出对象池的调用，gameobject没有实质的参与池的处理。

#### 3.PoolPreCreate.cs

PoolPreCreate.cs 文件，在start游戏开始时候，预先产生一些对象的脚本。

![image](https://github.com/cartzhang/pool_bench_test/blob/master/img/0.png)
图0
## 四、样例使用说明代码

HowToUse.cs文件中，说明怎么使用对象池。并起有UI文字提示。

![image](https://github.com/cartzhang/pool_bench_test/blob/master/img/4.png)
图4

TestEffect.cs脚本文件，测试在协同中一边申请，一边回池会不会造成错误的测试。

## 五、不足

#### 1.数量控制

没有对池内对象做最大数的处理，只是能实现功能，可以很简洁的使用。对于阶段性使用的对象，并没有做时间段内做整理和处理。

也就是说，若在A阶段，游戏场景中同时需要大量的某个池内O1对象，这时候就出现大量的AO1对象在内存中，但是过了A阶段，不需要A这么多O1对象时候，对象池内没有做操作和优化。

#### 2. 多线程处理
对于多线程的从对象池内取出对象和放进对象，没有做处理，就会出现竞争的情况。

暂时就想到了这些。

所以说，这个是一个simple pool for unity.
虽然有这些不足，但是对于简单的小游戏还是非常实用，用起来也非常简便，简单，简洁。希望大家能够喜欢。

## 六、工程分享地址：

github地址：
https://github.com/cartzhang/pool_bench_test/

可以下载Assets文件，然后用unity测试。

若有问题，请提交问题或代码，非常感谢！！！

## 七、 参考

[1] http://baike.baidu.com/link?url=56D15ulclFdO8d-HQ6KW0WQGjHwnzztof7FGYUyUC76Ui6J3I_0EU4qSfkXJRY7wDLjaCMN5UZKJb9Nt0WU-PT13GkFFtrBqGC6JIE9-zoIP8SB0KRhdZx8yk9kAFAaY

[2] http://www.111cn.net/net/160/81016.htm

[3] http://www.cnblogs.com/xinye/p/3907642.html

[4] https://msdn.microsoft.com/en-us/library/he2s3bh7(v=vs.110).aspx

[5] http://stackoverflow.com/questions/169973/when-should-i-use-a-list-vs-a-linkedlist