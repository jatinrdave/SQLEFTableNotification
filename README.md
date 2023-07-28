# [Building a Real-Time Database Notification Service with Change Tracking in C#](https://medium.com/@techbrainhub/building-a-real-time-database-notification-service-with-change-tracking-in-c-9512a2d14641)
**Change Tracking In SQL Server**
Change Tracking in SQL Server is a lightweight, built-in feature that helps track changes made to user tables in a database. It captures the fact that a row has been inserted, updated, or deleted, without storing the actual data changes. Instead, it records the minimum information needed to identify the modified rows.

**Enable Change Tracking**
Enabling Change Tracking in SQL Server involves a straightforward process. Here are the steps to enable Change Tracking for a specific table in a database:

--SQL Script for enable change tracking at database level.
ALTER DATABASE ECommerceDB
SET CHANGE_TRACKING = ON
(CHANGE_RETENTION = 1 DAYS, AUTO_CLEANUP = ON);

--SQL Script for enable change tracking at table level.
ALTER TABLE User  
ENABLE CHANGE_TRACKING  
WITH (TRACK_COLUMNS_UPDATED = ON ) 

_Retention Period (Optional): By default, SQL Server retains change tracking information for specified days_.

**Monitor Change Tracking**
After enabling Change Tracking, the system automatically maintains the change tracking information for the specified table(s). You can query the change tracking information periodically to synchronize changes with your application logic or for replication purposes.

Remember that Change Tracking captures minimal information about the changes (PK values and version numbers), so you may need additional queries to get the full details of the modified rows if required. Also, enabling Change Tracking on a table incurs some performance overhead, albeit relatively low, depending on the frequency of data changes.

**Access Tracked table records:**

SELECT ct.* FROM CHANGETABLE(CHANGES <dbname>,<lastversionno>) ct
Get Change Tracking Current Version:

SELECT ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0) as VersionCount

**Project Overview :**

![image](https://github.com/jatinrdave/SQLEFTableNotification/assets/15671321/eda1e961-48dd-4ebe-a110-10a197bc11b3)


This database notification service is encapsulated within the SqlDBNotificationService<TChangeTableEntity> class. This generic class is designed to work with any table entity, allowing flexibility and easy integration into various applications. The generic type constraint ensures that the entity class must be a reference type with a default constructor, providing a consistent structure for handling the table data.

The SqlDBNotificationService class implements the IDBNotificationService<TChangeTableEntity> interface, making it easy to integrate into existing projects. It also exposes events for error handling and receiving changed data. Service monitors database ChangeTable for specified interval and returns changed records for specified table till the last time monitored.


**Usage:**
![image](https://github.com/jatinrdave/SQLEFTableNotification/assets/15671321/5b04b30b-8372-4408-91e4-77be04e337d4)

* Initializing the Service:
Developers can create an instance of the SqlDBNotificationService class by providing the table name, database connection string, and an optional initial version for resuming monitoring.
* Subscribing to Events:
Applications can subscribe to the OnError and OnChanged events to handle errors and receive notifications when changes occur in the table.
* Starting and Stopping Monitoring:
The service can be started by calling the StartNotify() method, which initiates the polling process. The service can be stopped using the StopNotify() method.

**Features:**

* Asynchronous Event-Based Model: The notification service employs a pub-sub model, meaning that when changes occur in the monitored table, the service sends asynchronous notifications to the registered subscribers (i.e., the application).
* Query-Based Notifications: Unlike monitoring the entire table, our service allows the application to define a specific SQL query representing the data of interest. When relevant changes affect the query’s result set, a notification is triggered.
* Low Resource Utilization: Our service optimizes resource consumption by avoiding constant polling. Instead, it leverages SQL Server’s Change Tracking, which efficiently tracks and delivers notifications only when necessary.
* Flexible Initialization: Developers can specify the table to monitor, the connection string to the SQL Server database, an optional initial version for resuming monitoring, and a time interval for polling updates.
* Error Handling and Resilience: The service includes robust error handling to ensure the application remains stable even in the face of unexpected exceptions. If an error threshold is reached, the service gracefully stops and notifies subscribers.
* Easy Integration: The SqlDBNotificationService class implements the IDBNotificationService<TChangeTableEntity> interface, making it easy to integrate into existing projects. It also exposes events for error handling and receiving changed data.

**Conclusion:**

By utilizing the powerful combination of C#, SQL Server Change Tracking, and our custom SqlDBNotificationService, developers can easily add real-time database monitoring to their applications. The service's efficient event-driven model ensures timely and accurate updates without excessive resource consumption, enhancing application responsiveness and overall user experience.
