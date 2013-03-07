﻿using System;
using System.Collections.Generic;
using Caliburn.PresentationFramework.Screens;
using Machine.Specifications;
using NServiceBus.Profiler.Common.Models;
using NServiceBus.Profiler.Core;
using NServiceBus.Profiler.Desktop.Explorer;
using NServiceBus.Profiler.Desktop.Explorer.QueueExplorer;
using NServiceBus.Profiler.Desktop.Shell;
using NServiceBus.Profiler.Tests.Helpers;
using NSubstitute;

namespace NServiceBus.Profiler.Tests.QueueCreation
{
    [Subject("queue creation")]
    public class with_queue_creation_view_model
    {
        protected static QueueCreationViewModel Model;
        protected static IQueueManager QueueManager;
        protected static IQueueExplorerViewModel Explorer;
        protected static INetworkOperations Network;

        Establish context = () =>
        {
            QueueManager = Substitute.For<IQueueManager>();
            Explorer = Substitute.For<IQueueExplorerViewModel>();
            Network = Substitute.For<INetworkOperations>();
            Model = new QueueCreationViewModel(QueueManager, Explorer, Network);
            Network.GetMachines().Returns(new List<string> { Environment.MachineName, "AnotherMachine" });
            ((IActivate)Model).Activate();
        };

        It creates_transactional_queues_by_default = () => Model.IsTransactional.ShouldBeTrue();
        It has_not_selected_the_machine_name_by_default = () => Model.SelectedMachine.ShouldBeEmpty();
        It has_populated_list_of_available_machines = () => Model.Machines.Count.ShouldEqual(2);
        It can_not_accept_the_dialog_by_default = () => Model.CanAccept().ShouldBeFalse();
    }

    public class when_creating_a_queue : with_queue_creation_view_model
    {
        Because of = () =>
        {
            Model.SelectedMachine = Environment.MachineName;
            Model.QueueName = "TestQueue";
        };

        It should_allow_creating_the_queue = () => Model.CanAccept().ShouldBeTrue();
    }

    public class when_accepting_the_dialog : with_queue_creation_view_model
    {
        protected static TestConductorScreen Parent;
        protected static Queue AddedQueue;

        Establish context = () =>
        {
            Parent = new TestConductorScreen();
            Model.Parent = Parent;
            Model.SelectedMachine = Environment.MachineName;
            Model.QueueName = "TestQueue";
            Model.IsTransactional = true;
            
            AddedQueue = new Queue(Environment.MachineName, Model.QueueName);

            QueueManager.CreatePrivateQueue(Arg.Any<Queue>(), true).ReturnsForAnyArgs(AddedQueue);
        };

        Because of = () => Model.Accept();

        It should_create_the_queue_when_dialog_closes = () => QueueManager.Received().CreatePrivateQueue(Arg.Is<Queue>(q => q.Address.Equals("testqueue@.")), Arg.Is(true));
        It should_close_the_dialog = () => Model.IsActive.ShouldBeFalse();
    }
}