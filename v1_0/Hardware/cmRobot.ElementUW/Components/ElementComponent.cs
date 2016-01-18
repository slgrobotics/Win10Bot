using cmRobot.Element.Internal;
using System;
using System.Collections.Generic;

using slg.RobotAbstraction;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Components
{

	/// <summary>
    /// A component of the Element board.  All components must derive from
	/// this class.
	/// </summary>
    /// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="ElementComponent"]'/>
	public abstract class ElementComponent : IHardwareComponent
	{

		private Element element = null;
		private bool communicating = false;

        //private List<ElementComponentEventHandlerInfo> callbacks =
        //	new	List<ElementComponentEventHandlerInfo>();
		//private bool updating = false;


		internal ElementComponent()
		{
		}

		/// <summary>
        /// The <c>Element</c> instance that this component is attached to.
		/// </summary>
		public Element Element
		{
			get { return element; }

			set 
			{
				if (element != null)
				{
					OnDetachElement(element);
				}

				element = value;

				if (element != null)
				{
					OnAttachElement(element);
				}
			}
		}

        /*
        /// <summary>
        /// Signals the start of a multi-element update.  This will cause 
        /// the signalling of any events (fired via SignalEvent()) to be 
        /// postponed until UpdateComplete() is invoked.
        /// 
        /// This is used to delay firing events that would be caused 
        /// by updating some of the fields until a consistent view across 
        /// all of the fields
        /// </summary>
        protected void BeginUpdate()
        {
            updating = true;
        }

        /// <summary>
        /// Signals the end of a multi-element update.  See BeginUpdate()
        /// and SignalEvent().
        /// </summary>
        protected void UpdateComplete()
        {
            updating = false;
            foreach (ElementComponentEventHandlerInfo callback in callbacks)
            {
                SignalEvent(callback);
            }
            callbacks.Clear();
        }

        /// <summary>
        /// Enqueues a ElementComponentEventHandler in the element for this 
        /// component.  This allows events that are generated in a back ground
        /// thread to be singalled on the desired thread by calling the element's
        /// PumpEvents method.
        /// 
        /// See BeginUpdate() and UpdateComplete().
        /// </summary>
        /// <param name="handler"></param>
        protected void SignalEvent(ElementComponentEventHandler handler)
        {
            ElementComponentEventHandlerInfo callback = new ElementComponentEventHandlerInfo();
            callback.Callback = handler;
            callback.Component = this;

            if(updating)
            {
                callbacks.Add(callback);
            }
            else
            {
                SignalEvent(callback);
            }
        }
        */

        /// <summary>
        /// A flag indicating if communication with the <c>Element</c> board has
		/// been established.  Thies is set and cleared by <c>OnStartCommunication</c>
		/// and <c>OnStopCommunication</c>, respectively.
		/// </summary>
		protected bool Communicating
		{
			get { return communicating; }
		}
	
		/// <summary>
        /// Invoked when this componenet is attached to a (new) <c>Element</c>.  This
		/// attaches <c>OnStartCommunication</c> and <c>OnStopCommunication</c> to the
        /// corresponding events on the new element, unless it is a null value.
		/// </summary>
		/// <param name="element"></param>
		protected virtual void OnAttachElement(Element element)
		{
			element.CommunicationStarted += new HardwareComponentEventHandler(element_CommunicationStarted);
			element.CommunicationStopped += new HardwareComponentEventHandler(element_CommunicationStopped);
		}

		/// <summary>
		/// Invoked when this componenet is attached to a (new) <c>Element</c>.  This
		/// detaches <c>OnStartCommunication</c> and <c>OnStopCommunication</c> from the
        /// corresponding events on the old element, if previously attached.
		/// </summary>
		/// <param name="element"></param>
		protected virtual void OnDetachElement(Element element)
		{
			element.CommunicationStarted -= new HardwareComponentEventHandler(element_CommunicationStarted);
			element.CommunicationStopped -= new HardwareComponentEventHandler(element_CommunicationStopped);
		}

		/// <summary>
        /// Connected to the attached elements StartCommunication event.  Derived
		/// classes can override to perform any setup actions necessary once a 
        /// connection to the attached element board has been established.
		/// </summary>
		protected virtual void OnStartCommunication()
		{
		}

		/// <summary>
        /// Connected to the attached elements StopCommunication event.  Derived
		/// classes can override to perform any cleanup actions necessary after a 
        /// connection to the attached element board has been shutdown.
		/// </summary>
		protected virtual void OnStopCommunication()
		{
		}

		private void element_CommunicationStarted(IHardwareComponent sender)
		{
			communicating = true;
			OnStartCommunication();
		}

		private void element_CommunicationStopped(IHardwareComponent sender)
		{
			communicating = false;
			OnStopCommunication();
		}

        /*
        private void SignalEvent(ElementComponentEventHandlerInfo callback)
        {
            element.SignalEvent(callback);
        }
        */

    }

}
