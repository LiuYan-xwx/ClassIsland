﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个存储了课表、时间表和科目等信息的档案。
/// </summary>
public class Profile : ObservableRecipient
{
    private string _name = "";
    private ObservableDictionary<string, TimeLayout> _timeLayouts = new();
    private ObservableDictionary<string, ClassPlan> _classPlans = new();
    private ObservableDictionary<string, Subject> _subjects = new();
    private bool _isOverlayClassPlanEnabled = false;
    private string? _overlayClassPlanId = null;
    private ObservableCollection<Subject> _editingSubjects = new();
    private string? _tempClassPlanId;
    private DateTime _tempClassPlanSetupTime = DateTime.Now;

    /// <summary>
    /// 实例化对象
    /// </summary>
    public Profile()
    {
        Subjects.CollectionChanged += SubjectsOnCollectionChanged;
        PropertyChanging += OnPropertyChanging;
        PropertyChanged += OnPropertyChanged;
        UpdateEditingSubjects();
    }

    private void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(Subjects))
        {
            Subjects.CollectionChanged -= SubjectsOnCollectionChanged;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Subjects))
        {
            Subjects.CollectionChanged += SubjectsOnCollectionChanged;
            UpdateEditingSubjects();
        }
    }

    /// <summary>
    /// 重写指定时间表在所有课表中，某个时间点所有对应的课程的科目
    /// </summary>
    /// <param name="timeLayoutId">指定的时间表ID</param>
    /// <param name="timePoint">要覆写的课程对应的时间点</param>
    /// <param name="subjectId">要覆写成的科目ID</param>
    public void OverwriteAllClassPlanSubject(string timeLayoutId, TimeLayoutItem timePoint, string subjectId)
    {
        foreach (var classPlan in from i in ClassPlans where i.Value.TimeLayoutId == timeLayoutId select i.Value)
        {
            classPlan.RefreshClassesList();
            foreach (var i in from i in classPlan.Classes where i.CurrentTimeLayoutItem == timePoint select i)
            {
                i.SubjectId = subjectId;
            }
        }
    }

    private void UpdateEditingSubjects(NotifyCollectionChangedEventArgs? e=null)
    {
        if (e != null)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            EditingSubjects.CollectionChanged -= EditingSubjectsOnCollectionChanged;
            EditingSubjects = new ObservableCollection<Subject>(from i in Subjects select i.Value);
            EditingSubjects.CollectionChanged += EditingSubjectsOnCollectionChanged;
        }
    }

    private void EditingSubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Console.WriteLine($"{e.Action} {e.NewItems} {e.OldItems}");
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null)
                {
                    break;
                }
                foreach (var i in e.NewItems)
                {
                    Subjects[Guid.NewGuid().ToString()] = (Subject)i;
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null)
                {
                    break;
                }
                foreach (var i in e.OldItems)
                {
                    foreach (var k in Subjects.Where(k => k.Value == i))
                    {
                        Subjects.Remove(k.Key);
                        break;
                    }
                }

                //Subjects = ConfigureFileHelper.CopyObject(Subjects);
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        foreach (var i in Subjects)
        {
            Console.WriteLine($"{i.Key} {i.Value.Name}" );
        }
    }

    private void SubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEditingSubjects(e);
    }

    internal void NotifyPropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// 档案名称
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 存储的时间表字典，键为GUID
    /// </summary>
    public ObservableDictionary<string, TimeLayout> TimeLayouts
    {
        get => _timeLayouts;
        set
        {
            if (Equals(value, _timeLayouts)) return;
            _timeLayouts = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 存储的课表字典，键为GUID
    /// </summary>
    public ObservableDictionary<string, ClassPlan> ClassPlans
    {
        get => _classPlans;
        set
        {
            if (Equals(value, _classPlans)) return;
            _classPlans = value;
            OnPropertyChanged();
            _classPlans.CollectionChanged += delegate(object? sender, NotifyCollectionChangedEventArgs args)
            {
                RefreshTimeLayouts();
            };

            RefreshTimeLayouts();
        }
    }

    internal void RefreshTimeLayouts()
    {
        foreach (var i in _classPlans)
        {
            i.Value.TimeLayouts = TimeLayouts;
        }
    }

    /// <summary>
    /// 存储的科目字典，键为GUID
    /// </summary>
    public ObservableDictionary<string, Subject> Subjects
    {
        get => _subjects;
        set
        {
            if (Equals(value, _subjects)) return;
            _subjects = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 正在在档案编辑器编辑的科目信息
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<Subject> EditingSubjects
    {
        get => _editingSubjects;
        set
        {
            if (Equals(value, _editingSubjects)) return;
            _editingSubjects = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用临时层课表
    /// </summary>
    public bool IsOverlayClassPlanEnabled
    {
        get => _isOverlayClassPlanEnabled;
        set
        {
            if (value == _isOverlayClassPlanEnabled) return;
            _isOverlayClassPlanEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 临时层课表ID
    /// </summary>
    public string? OverlayClassPlanId
    {
        get => _overlayClassPlanId;
        set
        {
            if (value == _overlayClassPlanId) return;
            _overlayClassPlanId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 临时课表ID
    /// </summary>
    public string? TempClassPlanId
    {
        get => _tempClassPlanId;
        set
        {
            if (value == _tempClassPlanId) return;
            _tempClassPlanId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 临时课表设置时间
    /// </summary>
    public DateTime TempClassPlanSetupTime
    {
        get => _tempClassPlanSetupTime;
        set
        {
            if (value.Equals(_tempClassPlanSetupTime)) return;
            _tempClassPlanSetupTime = value;
            OnPropertyChanged();
        }
    }
}