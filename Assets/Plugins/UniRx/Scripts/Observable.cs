﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UniRx.Operators;

namespace UniRx
{
    // Standard Query Operators

    // onNext implementation guide. enclose otherFunc but onNext is not catch.
    // try{ otherFunc(); } catch { onError() }
    // onNext();

    public static partial class Observable
    {
        static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1); // from .NET 4.5

        public static IObservable<TR> Select<T, TR>(this IObservable<T> source, Func<T, TR> selector)
        {
            // sometimes cause "which no ahead of time (AOT) code was generated." on IL2CPP...

            //var select = source as ISelect<T>;
            //if (select != null)
            //{
            //    return select.CombineSelector(selector);
            //}

            // optimized path
            var whereObservable = source as UniRx.Operators.WhereObservable<T>;
            if (whereObservable != null)
            {
                return whereObservable.CombineSelector<TR>(selector);
            }

            return new SelectObservable<T, TR>(source, selector);
        }

        public static IObservable<TR> Select<T, TR>(this IObservable<T> source, Func<T, int, TR> selector)
        {
            return new SelectObservable<T, TR>(source, selector);
        }

        public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate)
        {
            // optimized path
            var whereObservable = source as UniRx.Operators.WhereObservable<T>;
            if (whereObservable != null)
            {
                return whereObservable.CombinePredicate(predicate);
            }

            var selectObservable = source as UniRx.Operators.ISelect<T>;
            if (selectObservable != null)
            {
                return selectObservable.CombinePredicate(predicate);
            }

            return new WhereObservable<T>(source, predicate);
        }

        public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, int, bool> predicate)
        {
            return new WhereObservable<T>(source, predicate);
        }

        /// <summary>
        /// Lightweight SelectMany for Single Async Operation.
        /// </summary>
        public static IObservable<TR> ContinueWith<T, TR>(this IObservable<T> source, IObservable<TR> other)
        {
            return ContinueWith(source, _ => other);
        }

        /// <summary>
        /// Lightweight SelectMany for Single Async Operation.
        /// </summary>
        public static IObservable<TR> ContinueWith<T, TR>(this IObservable<T> source, Func<T, IObservable<TR>> selector)
        {
            return new ContinueWithObservable<T, TR>(source, selector);
        }

        public static IObservable<TR> SelectMany<T, TR>(this IObservable<T> source, IObservable<TR> other)
        {
            return SelectMany(source, _ => other);
        }

        public static IObservable<TR> SelectMany<T, TR>(this IObservable<T> source, Func<T, IObservable<TR>> selector)
        {
            return new SelectManyObservable<T, TR>(source, selector);
        }

        public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, int, IObservable<TResult>> selector)
        {
            return new SelectManyObservable<TSource, TResult>(source, selector);
        }

        public static IObservable<TR> SelectMany<T, TC, TR>(this IObservable<T> source, Func<T, IObservable<TC>> collectionSelector, Func<T, TC, TR> resultSelector)
        {
            return new SelectManyObservable<T, TC, TR>(source, collectionSelector, resultSelector);
        }

        public static IObservable<TResult> SelectMany<TSource, TCollection, TResult>(this IObservable<TSource> source, Func<TSource, int, IObservable<TCollection>> collectionSelector, Func<TSource, int, TCollection, int, TResult> resultSelector)
        {
            return new SelectManyObservable<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            return new SelectManyObservable<TSource, TResult>(source, selector);
        }

        public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            return new SelectManyObservable<TSource, TResult>(source, selector);
        }

        public static IObservable<TResult> SelectMany<TSource, TCollection, TResult>(this IObservable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            return new SelectManyObservable<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        public static IObservable<TResult> SelectMany<TSource, TCollection, TResult>(this IObservable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, int, TCollection, int, TResult> resultSelector)
        {
            return new SelectManyObservable<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        public static IObservable<T[]> ToArray<T>(this IObservable<T> source)
        {
            return new ToArrayObservable<T>(source);
        }

        public static IObservable<IList<T>> ToList<T>(this IObservable<T> source)
        {
            return new ToListObservable<T>(source);
        }

        public static IObservable<T> Do<T>(this IObservable<T> source, IObserver<T> observer)
        {
            return new DoObserverObservable<T>(source, observer);
        }

        public static IObservable<T> Do<T>(this IObservable<T> source, Action<T> onNext)
        {
            return new DoObservable<T>(source, onNext, Stubs.Throw, Stubs.Nop);
        }

        public static IObservable<T> Do<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError)
        {
            return new DoObservable<T>(source, onNext, onError, Stubs.Nop);
        }

        public static IObservable<T> Do<T>(this IObservable<T> source, Action<T> onNext, Action onCompleted)
        {
            return new DoObservable<T>(source, onNext, Stubs.Throw, onCompleted);
        }

        public static IObservable<T> Do<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            return new DoObservable<T>(source, onNext, onError, onCompleted);
        }

        public static IObservable<T> DoOnError<T>(this IObservable<T> source, Action<Exception> onError)
        {
            return new DoOnErrorObservable<T>(source, onError);
        }

        public static IObservable<T> DoOnCompleted<T>(this IObservable<T> source, Action onCompleted)
        {
            return new DoOnCompletedObservable<T>(source, onCompleted);
        }

        public static IObservable<T> DoOnTerminate<T>(this IObservable<T> source, Action onTerminate)
        {
            return new DoOnTerminateObservable<T>(source, onTerminate);
        }

        public static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action onSubscribe)
        {
            return new DoOnSubscribeObservable<T>(source, onSubscribe);
        }

        public static IObservable<T> DoOnCancel<T>(this IObservable<T> source, Action onCancel)
        {
            return new DoOnCancelObservable<T>(source, onCancel);
        }

        public static IObservable<Notification<T>> Materialize<T>(this IObservable<T> source)
        {
            return new MaterializeObservable<T>(source);
        }

        public static IObservable<T> Dematerialize<T>(this IObservable<Notification<T>> source)
        {
            return new DematerializeObservable<T>(source);
        }

        public static IObservable<T> DefaultIfEmpty<T>(this IObservable<T> source)
        {
            return new DefaultIfEmptyObservable<T>(source, default(T));
        }

        public static IObservable<T> DefaultIfEmpty<T>(this IObservable<T> source, T defaultValue)
        {
            return new DefaultIfEmptyObservable<T>(source, defaultValue);
        }

        public static IObservable<TSource> Distinct<TSource>(this IObservable<TSource> source)
        {
<<<<<<< HEAD:Assets/Plugins/UniRx/Scripts/Observable.cs
#if !UniRxLibrary
            var comparer = UnityEqualityComparer.GetDefault<TSource>();
#else
            var comparer = EqualityComparer<TSource>.Default;
#endif

            return new DistinctObservable<TSource>(source, comparer);
=======
            return Distinct<TSource>(source, (IEqualityComparer<TSource>)null);
>>>>>>> parent of fa1993f... Distinct, DistinctUntilChanged #97:Assets/UniRx/Scripts/Observable.cs
        }

        public static IObservable<TSource> Distinct<TSource>(this IObservable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            // don't use x => x for avoid iOS AOT issue.
            return Observable.Create<TSource>(observer =>
            {
                var hashSet = (comparer == null)
                    ? new HashSet<TSource>()
                    : new HashSet<TSource>(comparer);
                return source.Subscribe(
                    x =>
                    {
                        var key = default(TSource);
                        var hasAdded = false;

                        try
                        {
                            key = x;
                            hasAdded = hashSet.Add(key);
                        }
                        catch (Exception exception)
                        {
                            observer.OnError(exception);
                            return;
                        }

                        if (hasAdded)
                        {
                            observer.OnNext(x);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                );
            });
        }

        public static IObservable<TSource> Distinct<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector)
        {
<<<<<<< HEAD:Assets/Plugins/UniRx/Scripts/Observable.cs
#if !UniRxLibrary
            var comparer = UnityEqualityComparer.GetDefault<TKey>();
#else
            var comparer = EqualityComparer<TKey>.Default;
#endif

            return new DistinctObservable<TSource, TKey>(source, keySelector, comparer);
=======
            return Distinct(source, keySelector, null);
>>>>>>> parent of fa1993f... Distinct, DistinctUntilChanged #97:Assets/UniRx/Scripts/Observable.cs
        }

        public static IObservable<TSource> Distinct<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return Observable.Create<TSource>(observer =>
            {
                var hashSet = (comparer == null)
                    ? new HashSet<TKey>()
                    : new HashSet<TKey>(comparer);
                return source.Subscribe(
                    x =>
                    {
                        var key = default(TKey);
                        var hasAdded = false;

                        try
                        {
                            key = keySelector(x);
                            hasAdded = hashSet.Add(key);
                        }
                        catch (Exception exception)
                        {
                            observer.OnError(exception);
                            return;
                        }

                        if (hasAdded)
                        {
                            observer.OnNext(x);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                );
            });
        }

        public static IObservable<T> DistinctUntilChanged<T>(this IObservable<T> source)
        {
<<<<<<< HEAD:Assets/Plugins/UniRx/Scripts/Observable.cs
#if !UniRxLibrary
            var comparer = UnityEqualityComparer.GetDefault<T>();
#else
            var comparer = EqualityComparer<T>.Default;
#endif

            return new DistinctUntilChangedObservable<T>(source, comparer);
=======
            return source.DistinctUntilChanged((IEqualityComparer<T>)null);
>>>>>>> parent of fa1993f... Distinct, DistinctUntilChanged #97:Assets/UniRx/Scripts/Observable.cs
        }

        public static IObservable<T> DistinctUntilChanged<T>(this IObservable<T> source, IEqualityComparer<T> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");

            return Observable.Create<T>(observer =>
            {
                var isFirst = true;
                var prevKey = default(T);
                return source.Subscribe(x =>
                {
                    T currentKey;
                    try
                    {
                        currentKey = x;
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        return;
                    }

                    var sameKey = false;
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        try
                        {
                            if (comparer == null)
                            {
                                if (currentKey == null)
                                {
                                    sameKey = (prevKey == null);
                                }
                                else
                                {
                                    sameKey = currentKey.Equals(prevKey);
                                }
                            }
                            else
                            {
                                sameKey = comparer.Equals(currentKey, prevKey);
                            }
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return;
                        }
                    }
                    if (!sameKey)
                    {
                        prevKey = currentKey;
                        observer.OnNext(x);
                    }
                }, observer.OnError, observer.OnCompleted);
            });
        }

        public static IObservable<T> DistinctUntilChanged<T, TKey>(this IObservable<T> source, Func<T, TKey> keySelector)
        {
<<<<<<< HEAD:Assets/Plugins/UniRx/Scripts/Observable.cs
#if !UniRxLibrary
            var comparer = UnityEqualityComparer.GetDefault<TKey>();
#else
            var comparer = EqualityComparer<TKey>.Default;
#endif

            return new DistinctUntilChangedObservable<T, TKey>(source, keySelector, comparer);
=======
            return DistinctUntilChanged<T, TKey>(source, keySelector, null);
>>>>>>> parent of fa1993f... Distinct, DistinctUntilChanged #97:Assets/UniRx/Scripts/Observable.cs
        }

        public static IObservable<T> DistinctUntilChanged<T, TKey>(this IObservable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");

            return Observable.Create<T>(observer =>
            {
                var isFirst = true;
                var prevKey = default(TKey);
                return source.Subscribe(x =>
                {
                    TKey currentKey;
                    try
                    {
                        currentKey = keySelector(x);
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        return;
                    }

                    var sameKey = false;
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        try
                        {
                            sameKey = (comparer == null)
                                ? currentKey.Equals(prevKey)
                                : comparer.Equals(currentKey, prevKey);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return;
                        }
                    }
                    if (!sameKey)
                    {
                        prevKey = currentKey;
                        observer.OnNext(x);
                    }
                }, observer.OnError, observer.OnCompleted);
            });
        }

        public static IObservable<T> IgnoreElements<T>(this IObservable<T> source)
        {
            return new IgnoreElementsObservable<T>(source);
        }

        public static IObservable<Unit> ForEachAsync<T>(this IObservable<T> source, Action<T> onNext)
        {
            return new ForEachAsyncObservable<T>(source, onNext);
        }

        public static IObservable<Unit> ForEachAsync<T>(this IObservable<T> source, Action<T, int> onNext)
        {
            return new ForEachAsyncObservable<T>(source, onNext);
        }
    }
}